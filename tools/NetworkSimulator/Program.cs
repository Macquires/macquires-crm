using System.Collections.Concurrent;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var subscribers = new ConcurrentDictionary<string, SubscriberState>(StringComparer.Ordinal);
var chaos = new ChaosSettings { Enabled = false };

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "nSuite-NetworkSimulator" }));

app.MapGet("/admin/chaos", () => Results.Ok(chaos));
app.MapPost("/admin/chaos", (ChaosSettings settings) =>
{
    chaos.Enabled = settings.Enabled;
    chaos.TimeoutProbability = settings.TimeoutProbability;
    chaos.RateLimitProbability = settings.RateLimitProbability;
    chaos.ResponseDelayMs = settings.ResponseDelayMs;
    chaos.CorruptPayload = settings.CorruptPayload;
    return Results.Ok(chaos);
});

app.MapGet("/cbs/subscribers/{msisdn}/balance", (string msisdn) =>
{
    if (ShouldFail(chaos, out var fail)) return fail;
    var state = subscribers.GetOrAdd(msisdn, _ => new SubscriberState { Msisdn = msisdn, Balance = 1000m });
    return Results.Ok(new { msisdn, balance = state.Balance, currency = "SYP" });
});

app.MapPost("/cbs/subscribers/{msisdn}/provision", async (string msisdn, ProvisionRequest request) =>
{
    if (await ChaosGate.ApplyAsync(chaos, corruptAllowed: true)) return ChaosGate.Result!;
    var state = subscribers.GetOrAdd(msisdn, _ => new SubscriberState { Msisdn = msisdn });
    state.Balance = request.InitialDeposit ?? state.Balance;
    state.Status = "ACTIVE";
    return Results.Ok(new { success = true, message = $"CBS provisioned {msisdn}", accountId = $"CBS-{msisdn}" });
});

app.MapPost("/cbs/subscribers/{msisdn}/reverse", (string msisdn) =>
{
    if (ShouldFail(chaos, out var fail)) return fail;
    if (subscribers.TryGetValue(msisdn, out var state))
    {
        state.Status = "REVERSED";
    }
    return Results.Ok(new { success = true, message = $"CBS reversed {msisdn}" });
});

app.MapPost("/cbs/subscribers/{msisdn}/adjust-balance", (string msisdn, AdjustBalanceRequest request) =>
{
    if (ShouldFail(chaos, out var fail)) return fail;
    var state = subscribers.GetOrAdd(msisdn, _ => new SubscriberState { Msisdn = msisdn, Balance = 0 });
    state.Balance = request.NewBalance;
    return Results.Ok(new { success = true, balance = state.Balance });
});

app.MapPost("/cbs/subscribers/{msisdn}/recharge", (string msisdn, RechargeRequest request) =>
{
    if (ShouldFail(chaos, out var fail)) return fail;
    var state = subscribers.GetOrAdd(msisdn, _ => new SubscriberState { Msisdn = msisdn, Balance = 1000m });
    state.Balance += request.Amount;
    return Results.Ok(new { success = true, balance = state.Balance, msisdn });
});

app.MapGet("/hlr/subscribers/{msisdn}/status", (string msisdn) =>
{
    if (ShouldFail(chaos, out var fail)) return fail;
    var state = subscribers.GetOrAdd(msisdn, _ => new SubscriberState { Msisdn = msisdn });
    var hlrState = ResolveHlrState(msisdn, state.Status);
    return Results.Ok(new
    {
        msisdn,
        hlrSubscriberState = hlrState,
        isOnline = hlrState == "ACTIVE",
        location = "Damascus-GMSC-01",
        activeImsi = "417011234567890"
    });
});

app.MapPost("/hlr/subscribers/{msisdn}/provision", async (string msisdn, HlrProvisionRequest request) =>
{
    if (await ChaosGate.ApplyAsync(chaos, corruptAllowed: true)) return ChaosGate.Result!;
    var state = subscribers.GetOrAdd(msisdn, _ => new SubscriberState { Msisdn = msisdn });
    state.Status = request.Command?.Contains("SUSPEND", StringComparison.OrdinalIgnoreCase) == true ? "SUSPENDED" : "ACTIVE";
    return Results.Ok(new { success = true, hlrState = state.Status });
});

app.MapPost("/sms/send", (SmsRequest request) =>
{
    if (ShouldFail(chaos, out var fail)) return fail;
    return Results.Ok(new { success = true, messageId = Guid.CreateVersion7().ToString(), to = request.To, body = request.Body });
});

app.MapPost("/mnp/port-in", (MnpPortInRequest request) =>
{
    if (ShouldFail(chaos, out var fail)) return fail;
    var externalId = $"MNP-SIM-{request.OperationNumber ?? Guid.NewGuid().ToString("N")[..8]}";
    return Results.Ok(new { success = true, message = "MNP port-in accepted", externalCorrelationId = externalId });
});

app.MapGet("/mnp/orders/{externalId}/status", (string externalId) =>
{
    if (ShouldFail(chaos, out var fail)) return fail;
    return Results.Ok(new { status = "Completed", message = $"Order {externalId} completed" });
});

app.Run();

static string ResolveHlrState(string msisdn, string crmStatus)
{
    if (msisdn == "0939000002") return "ACTIVE";
    if (msisdn.EndsWith('3')) return "NOT_PROVISIONED";
    if (msisdn.EndsWith('5')) return "SUSPENDED";
    return crmStatus switch
    {
        "SUSPENDED" => "SUSPENDED",
        "TERMINATED" or "REVERSED" => "INACTIVE",
        _ => "ACTIVE"
    };
}

static bool ShouldFail(ChaosSettings chaos, out IResult failResult)
{
    failResult = Results.Ok();
    if (!chaos.Enabled) return false;
    var roll = Random.Shared.Next(100);
    if (roll < chaos.TimeoutProbability)
    {
        failResult = Results.StatusCode(504);
        return true;
    }
    if (roll < chaos.TimeoutProbability + chaos.RateLimitProbability)
    {
        failResult = Results.StatusCode(429);
        return true;
    }
    return false;
}

static class ChaosGate
{
    public static IResult? Result { get; private set; }

    public static async Task<bool> ApplyAsync(ChaosSettings chaos, bool corruptAllowed)
    {
        Result = null;
        if (chaos.Enabled && chaos.ResponseDelayMs > 0)
        {
            await Task.Delay(chaos.ResponseDelayMs);
        }

        if (corruptAllowed && chaos.Enabled && chaos.CorruptPayload)
        {
            Result = Results.Content("{\"success\":true,\"hlrState\":\"ACT", "application/json", statusCode: 200);
            return true;
        }

        if (ShouldFailStatic(chaos, out var fail))
        {
            Result = fail;
            return true;
        }

        return false;
    }

    private static bool ShouldFailStatic(ChaosSettings chaos, out IResult failResult)
    {
        failResult = Results.Ok();
        if (!chaos.Enabled) return false;
        var roll = Random.Shared.Next(100);
        if (roll < chaos.TimeoutProbability)
        {
            failResult = Results.StatusCode(504);
            return true;
        }
        if (roll < chaos.TimeoutProbability + chaos.RateLimitProbability)
        {
            failResult = Results.StatusCode(429);
            return true;
        }
        return false;
    }
}

sealed class SubscriberState
{
    public string Msisdn { get; set; } = "";
    public decimal Balance { get; set; }
    public string Status { get; set; } = "ACTIVE";
}

sealed class ChaosSettings
{
    public bool Enabled { get; set; }
    public int TimeoutProbability { get; set; } = 5;
    public int RateLimitProbability { get; set; } = 5;
    public int ResponseDelayMs { get; set; }
    public bool CorruptPayload { get; set; }
}

sealed record ProvisionRequest(decimal? InitialDeposit);
sealed record AdjustBalanceRequest(decimal NewBalance, string? Reason, string? IdempotencyKey);
sealed record RechargeRequest(decimal Amount);
sealed record HlrProvisionRequest(string? Command, string? Imsi, string? Iccid);
sealed record SmsRequest(string To, string Body);
sealed record MnpPortInRequest(string? OperationId, string? OperationNumber, string? CurrentMsisdn, string? PortInMsisdn, string? DonorOperatorCode, string? AgencyReference, string? CorrelationId);

public partial class Program;
