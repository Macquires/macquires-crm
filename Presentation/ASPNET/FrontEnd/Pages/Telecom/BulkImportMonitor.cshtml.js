const App = {
    setup() {
        const pick = (o, ...keys) => {
            if (!o) return undefined;
            for (const k of keys) {
                if (o[k] !== undefined && o[k] !== null) return o[k];
            }
            return undefined;
        };

        const state = Vue.reactive({
            jobs: [],
            loading: true,
            uploading: false,
            uploadJobType: '0',
            selectedFile: null,
            errorJobId: null,
            errorJobLabel: '',
            errorRows: [],
            errorsLoading: false,
            locale: document.documentElement.lang?.startsWith('en') ? 'en' : 'ar',
        });

        const mainGridRef = Vue.ref(null);
        const errorsGridRef = Vue.ref(null);
        const errorsModalRef = Vue.ref(null);
        const fileInputRef = Vue.ref(null);
        let mainGrid = null;
        let errorsGrid = null;
        let errorsModal = null;
        let pollTimer = null;

        const t = (key, fallback) => {
            try {
                const parts = key.split('.');
                let node = window.__telecomLocale?.telecom;
                for (const p of parts) {
                    node = node?.[p];
                }
                return node || fallback;
            } catch {
                return fallback;
            }
        };

        const statusLabels = () => ({
            0: t('bulkImport.status.pending', 'قيد الانتظار'),
            1: t('bulkImport.status.processing', 'قيد المعالجة'),
            2: t('bulkImport.status.completed', 'مكتمل'),
            3: t('bulkImport.status.failed', 'فشل'),
            4: t('bulkImport.status.partial', 'مكتمل جزئياً'),
        });

        const jobStatusLabel = (st) => statusLabels()[Number(st)] || String(st);

        const jobStatusBadge = (st) => {
            const s = Number(st);
            if (s === 2) return 'bg-success';
            if (s === 4) return 'bg-warning text-dark';
            if (s === 3) return 'bg-danger';
            if (s === 1) return 'bg-primary';
            return 'bg-secondary';
        };

        const progressPct = (j) => {
            const total = Number(pick(j, 'totalRows', 'TotalRows')) || 0;
            const processed = Number(pick(j, 'processedRows', 'ProcessedRows')) || 0;
            if (total <= 0) return 0;
            return Math.min(100, Math.round((processed / total) * 100));
        };

        const formatDate = (d) => {
            if (!d) return 'â€”';
            try {
                return new Date(d).toLocaleString(state.locale === 'en' ? 'en-GB' : 'ar-SY');
            } catch {
                return String(d);
            }
        };

        const normalizeJob = (raw) => {
            const n = {
                id: pick(raw, 'id', 'Id') || '',
                jobStatus: Number(pick(raw, 'jobStatus', 'JobStatus') ?? 0),
                jobType: Number(pick(raw, 'jobType', 'JobType') ?? 0),
                fileName: pick(raw, 'fileName', 'FileName') || pick(raw, 'id', 'Id')?.slice(0, 12),
                totalRows: Number(pick(raw, 'totalRows', 'TotalRows') ?? 0),
                processedRows: Number(pick(raw, 'processedRows', 'ProcessedRows') ?? 0),
                successCount: Number(pick(raw, 'successCount', 'SuccessCount') ?? 0),
                errorCount: Number(pick(raw, 'errorCount', 'ErrorCount') ?? 0),
                createdAtUtc: pick(raw, 'createdAtUtc', 'CreatedAtUtc'),
            };
            return {
                ...n,
                jobStatusLabel: jobStatusLabel(n.jobStatus),
                jobStatusBadgeClass: 'badge ' + jobStatusBadge(n.jobStatus),
                progressPct: progressPct(n),
                createdAtDisplay: formatDate(n.createdAtUtc),
            };
        };

        const hasActiveJobs = () =>
            state.jobs.some((j) => j.jobStatus === 0 || j.jobStatus === 1);

        const mergeJobs = (incoming) => {
            const map = new Map(state.jobs.map((j) => [j.id, j]));
            for (const row of incoming) {
                const n = normalizeJob(row);
                const prev = map.get(n.id);
                map.set(n.id, prev ? { ...prev, ...n } : n);
            }
            state.jobs = Array.from(map.values()).sort(
                (a, b) => new Date(b.createdAtUtc || 0) - new Date(a.createdAtUtc || 0)
            );
        };

        const loadJobs = async (silent = false) => {
            if (!silent) state.loading = true;
            try {
                const res = await AxiosManager.get('/Telecom/GetInventoryBulkImportJobList?take=50', {});
                const list = res?.data?.content?.data ?? res?.data?.content?.Data ?? [];
                mergeJobs(list);
                if (mainGrid) {
                    mainGrid.dataSource = state.jobs;
                }
                syncPolling();
            } catch {
                if (!silent) state.jobs = [];
            } finally {
                if (!silent) state.loading = false;
            }
        };

        const syncPolling = () => {
            if (hasActiveJobs() && !pollTimer) {
                pollTimer = setInterval(() => loadJobs(true), 3000);
            } else if (!hasActiveJobs() && pollTimer) {
                clearInterval(pollTimer);
                pollTimer = null;
            }
        };

        const onFileSelected = (e) => {
            state.selectedFile = e.target?.files?.[0] || null;
        };

        const downloadTemplate = async (jobType) => {
            const token = typeof StorageManager !== 'undefined' ? StorageManager.getAccessToken() : null;
            const url = '/api/Telecom/DownloadBulkImportTemplate?jobType=' + encodeURIComponent(jobType);
            try {
                const res = await fetch(url, {
                    headers: token ? { Authorization: 'Bearer ' + token } : {},
                });
                if (!res.ok) throw new Error('Template download failed');
                const blob = await res.blob();
                const name = res.headers.get('Content-Disposition')?.match(/filename="?([^";]+)"?/)?.[1]
                    || 'Template.csv';
                const a = document.createElement('a');
                a.href = URL.createObjectURL(blob);
                a.download = name;
                a.click();
                URL.revokeObjectURL(a.href);
            } catch {
                if (window.Swal) Swal.fire({ icon: 'error', title: 'تعذر تحميل القالب' });
            }
        };

        const uploadFile = async () => {
            if (!state.selectedFile) return;
            state.uploading = true;
            try {
                const fd = new FormData();
                fd.append('file', state.selectedFile);
                fd.append('jobType', state.uploadJobType);
                const token = typeof StorageManager !== 'undefined' ? StorageManager.getAccessToken() : null;
                const res = await fetch('/api/Telecom/UploadInventoryBulkImport', {
                    method: 'POST',
                    headers: token ? { Authorization: 'Bearer ' + token } : {},
                    body: fd,
                });
                const json = await res.json();
                if (json?.code === 200) {
                    if (window.Swal) {
                        Swal.fire({ icon: 'success', title: t('bulkImport.uploadOk', 'تم الرفع'), timer: 2500, showConfirmButton: false });
                    }
                    state.selectedFile = null;
                    if (fileInputRef.value) fileInputRef.value.value = '';
                    await loadJobs();
                } else {
                    const msg = json?.message || json?.content?.message || 'فشل الرفع — تحقق من أعمدة القالب';
                    if (window.Swal) Swal.fire({ icon: 'error', title: msg, text: msg });
                    await loadJobs();
                }
            } catch (err) {
                const msg = err?.message || 'فشل الرفع';
                if (window.Swal) Swal.fire({ icon: 'error', title: msg });
            } finally {
                state.uploading = false;
            }
        };

        const loadErrors = async (jobId) => {
            state.errorsLoading = true;
            try {
                const res = await AxiosManager.get(
                    '/Telecom/GetInventoryBulkImportJobErrors?jobId=' + encodeURIComponent(jobId) + '&take=500',
                    {}
                );
                const list = res?.data?.content?.data ?? res?.data?.content?.Data ?? [];
                state.errorRows = list.map((r) => {
                    const errorMessageAr = pick(r, 'errorMessageAr', 'ErrorMessageAr') || '';
                    const errorMessageEn = pick(r, 'errorMessageEn', 'ErrorMessageEn') || '';
                    return {
                        rowNumber: pick(r, 'rowNumber', 'RowNumber'),
                        identifier: pick(r, 'identifier', 'Identifier'),
                        errorMessageAr,
                        errorMessageEn,
                        errorMessageDisplay:
                            state.locale === 'en'
                                ? errorMessageEn || errorMessageAr
                                : errorMessageAr || errorMessageEn,
                    };
                });
                if (errorsGrid) {
                    errorsGrid.dataSource = state.errorRows;
                }
            } finally {
                state.errorsLoading = false;
            }
        };

        const openErrors = async (job) => {
            state.errorJobId = job.id;
            state.errorJobLabel = job.fileName || job.id;
            await loadErrors(job.id);
            errorsModal?.show();
        };

        const exportErrors = async () => {
            if (!state.errorJobId) return;
            const token = typeof StorageManager !== 'undefined' ? StorageManager.getAccessToken() : null;
            const url = '/api/Telecom/ExportInventoryBulkImportErrors?jobId=' + encodeURIComponent(state.errorJobId);
            try {
                const res = await fetch(url, {
                    headers: token ? { Authorization: 'Bearer ' + token } : {},
                });
                if (!res.ok) throw new Error('Export failed');
                const blob = await res.blob();
                const a = document.createElement('a');
                a.href = URL.createObjectURL(blob);
                a.download = 'bulk-import-errors.csv';
                a.click();
                URL.revokeObjectURL(a.href);
            } catch {
                if (window.Swal) Swal.fire({ icon: 'error', title: 'تعذر التصدير' });
            }
        };

        const buildMainGrid = () => {
            if (!mainGridRef.value || mainGrid) return;
            mainGrid = new ej.grids.Grid({
                height: '520px',
                allowPaging: true,
                pageSettings: { pageSize: 15 },
                allowSorting: true,
                dataSource: state.jobs,
                rowSelected: (args) => {
                    const row = args?.data;
                    if (row && Number(row.errorCount) > 0) {
                        openErrors(row);
                    }
                },
                columns: [
                    { field: 'fileName', headerText: t('bulkImport.columns.job', 'الملف'), width: 180 },
                    {
                        field: 'jobStatusLabel',
                        headerText: t('bulkImport.columns.status', 'الحالة'),
                        width: 130,
                        template: '#bulkJobStatusTemplate',
                    },
                    {
                        field: 'progressPct',
                        headerText: t('bulkImport.columns.progress', 'التقدّم'),
                        width: 160,
                        template: '#bulkProgressTemplate',
                    },
                    {
                        field: 'successCount',
                        headerText: t('bulkImport.columns.success', 'نجاح'),
                        width: 90,
                        textAlign: 'Right',
                        template: '#bulkSuccessCountTemplate',
                    },
                    {
                        field: 'errorCount',
                        headerText: t('bulkImport.columns.errors', 'أخطاء'),
                        width: 90,
                        textAlign: 'Right',
                    },
                    {
                        field: 'createdAtDisplay',
                        headerText: t('bulkImport.columns.created', 'أُنشئ'),
                        width: 150,
                    },
                ],
            });
            mainGrid.appendTo(mainGridRef.value);
        };

        const buildErrorsGrid = () => {
            if (!errorsGridRef.value || errorsGrid) return;
            errorsGrid = new ej.grids.Grid({
                height: '360px',
                allowPaging: true,
                pageSettings: { pageSize: 20 },
                dataSource: state.errorRows,
                columns: [
                    { field: 'rowNumber', headerText: 'Row', width: 70 },
                    { field: 'identifier', headerText: 'ID', width: 120 },
                    {
                        field: 'errorMessageDisplay',
                        headerText: state.locale === 'en' ? 'Error' : 'خطأ',
                    },
                ],
            });
            errorsGrid.appendTo(errorsGridRef.value);
        };

        Vue.onMounted(async () => {
            if (typeof SecurityManager !== 'undefined') {
                await SecurityManager.authorizePage(['TelecomBackOffice', 'TelecomAdmin']);
            }
            buildMainGrid();
            buildErrorsGrid();
            if (errorsModalRef.value) {
                errorsModal = new bootstrap.Modal(errorsModalRef.value);
            }
            await loadJobs();
            if (typeof hideSpinnerAndShowContent === 'function') hideSpinnerAndShowContent();
        });

        Vue.onUnmounted(() => {
            if (pollTimer) clearInterval(pollTimer);
        });

        return {
            ...Vue.toRefs(state),
            mainGridRef,
            errorsGridRef,
            errorsModalRef,
            fileInputRef,
            loadJobs,
            onFileSelected,
            uploadFile,
            downloadTemplate,
            exportErrors,
            jobStatusLabel,
            jobStatusBadge,
            progressPct,
            formatDate,
        };
    },
};

Vue.createApp(App).mount('#app');
