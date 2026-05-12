// Demo-only fictional Syria Telecom-style catalog for presentation datasets.
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos
{
    public class ProductSeeder
    {
        private readonly ICommandRepository<Product> _productRepository;
        private readonly ICommandRepository<ProductGroup> _productGroupRepository;
        private readonly ICommandRepository<UnitMeasure> _unitMeasureRepository;
        private readonly NumberSequenceService _numberSequenceService;
        private readonly IUnitOfWork _unitOfWork;

        public ProductSeeder(
            ICommandRepository<Product> productRepository,
            ICommandRepository<ProductGroup> productGroupRepository,
            ICommandRepository<UnitMeasure> unitMeasureRepository,
            NumberSequenceService numberSequenceService,
            IUnitOfWork unitOfWork
        )
        {
            _productRepository = productRepository;
            _productGroupRepository = productGroupRepository;
            _unitMeasureRepository = unitMeasureRepository;
            _numberSequenceService = numberSequenceService;
            _unitOfWork = unitOfWork;
        }

        public async Task GenerateDataAsync()
        {
            var productGroups = await _productGroupRepository.GetQuery().ToListAsync();
            var measures = (await _unitMeasureRepository.GetQuery().Where(x => x.Name == "unit").ToListAsync()).Select(x => x.Id).ToArray();

            var groupMapping = new Dictionary<string, string>();

            foreach (var pg in productGroups)
            {
                if (!string.IsNullOrEmpty(pg.Name) && pg.Id != null)
                {
                    groupMapping.Add(pg.Name, pg.Id);
                }
            }

            var products = new List<Product>
            {
                // Mobile lines (virtual)
                new Product { Name = "يا هلا شباب (Prepaid)", ServiceCode = "YAHALA_SHABAB", UnitPrice = 150.0, Physical = false, ProductGroupId = groupMapping["Mobile Lines"] },
                new Product { Name = "سوريا تيليكوم ميكس (Hybrid)", ServiceCode = "SYR_MIX_HYBRID", UnitPrice = 250.0, Physical = false, ProductGroupId = groupMapping["Mobile Lines"] },
                new Product { Name = "خط سوريا تيليكوم فاتورة — بلاتيني (Postpaid)", ServiceCode = "SYR_POST_PLAT", UnitPrice = 400.0, Physical = false, ProductGroupId = groupMapping["Mobile Lines"] },

                // Data packages
                new Product { Name = "سَبَا 10GB (باقة بيانات)", ServiceCode = "SABA_10GB", UnitPrice = 35.0, Physical = false, ProductGroupId = groupMapping["Data Packages"] },
                new Product { Name = "باقة أعمال برو 50GB", ServiceCode = "BUSINESS_PRO_50", UnitPrice = 120.0, Physical = false, ProductGroupId = groupMapping["Data Packages"] },
                new Product { Name = "باقة ليلية غير محدودة", ServiceCode = "NIGHT_UNL", UnitPrice = 45.0, Physical = false, ProductGroupId = groupMapping["Data Packages"] },

                // Hardware
                new Product { Name = "راوتر سوريا تيليكوم 4G/5G", ServiceCode = "HW_ROUTER_5G", UnitPrice = 185.0, Physical = true, ProductGroupId = groupMapping["Hardware"] },
                new Product { Name = "Wingle (USB Modem)", ServiceCode = "HW_WINGLE", UnitPrice = 95.0, Physical = true, ProductGroupId = groupMapping["Hardware"] },

                // Ancillary services (for MIS / adjustments demos)
                new Product { Name = "رسوم تفعيل خط", ServiceCode = "SRV_ACTIVATION", UnitPrice = 25.0, Physical = false, ProductGroupId = groupMapping["Service"] },
                new Product { Name = "خصم ترويجي", ServiceCode = "SRV_PROMO_DISC", UnitPrice = -15.0, Physical = false, ProductGroupId = groupMapping["Service"] }
            };

            foreach (var product in products)
            {
                product.Number = _numberSequenceService.GenerateNumber(nameof(Product), "", "ART");
                product.UnitMeasureId = measures[0];
                product.Physical ??= true;

                await _productRepository.CreateAsync(product);
            }

            await _unitOfWork.SaveAsync();
        }
    }
}
