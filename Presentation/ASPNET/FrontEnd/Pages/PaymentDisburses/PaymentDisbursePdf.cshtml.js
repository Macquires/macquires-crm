const App = {
    setup() {
        const state = Vue.reactive({
            company: {
                name: '',
                emailAddress: '',
                phoneNumber: '',
                street: '',
                city: '',
                state: '',
                zipCode: '',
                country: ''
            },
            companyAddress: '',
            vendor: {
                name: '',
                street: '',
                city: '',
                state: '',
                zipCode: '',
                country: '',
                emailAddress: '',
                phoneNumber: ''
            },
            billNumber: '',
            vendorAddress: '',
            paymentDisburseNumber: '',
            paymentDisburseDate: '',
            paymentDisburseCurrency: '',
            paymentMethod: '',
            paymentAmount: '',
            subTotal: '',
            tax: '',
            totalAmount: '',
            items: [],
            isDownloading: false
        });

        const services = {
            getPDFData: async (id) => {
                try {
                    const response = await AxiosManager.get('/PaymentDisburse/GetPaymentDisburseSingle?id=' + id, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
        };

        const methods = {
            populatePDFData: async (id) => {
                const response = await services.getPDFData(id);
                const pdfData = response?.data?.content?.data || {};

                state.billNumber = pdfData?.bill?.number;
                state.paymentMethod = pdfData?.paymentMethod?.name;
                state.paymentAmount = NumberFormatManager.formatToLocale(pdfData?.paymentAmount) || '';
                state.items = pdfData?.bill?.purchaseOrder?.purchaseOrderItemList;
                state.vendor = pdfData?.bill?.purchaseOrder?.vendor || {};
                state.paymentDisburseNumber = pdfData.number || '';
                state.paymentDisburseDate = DateFormatManager.formatToLocale(pdfData.paymentDate) || '';
                state.paymentDisburseCurrency = StorageManager.getCompany()?.currency || '';
                state.subTotal = NumberFormatManager.formatToLocale(pdfData?.bill?.purchaseOrder?.beforeTaxAmount) || '';
                state.tax = NumberFormatManager.formatToLocale(pdfData?.bill?.purchaseOrder?.taxAmount) || '';
                state.totalAmount = NumberFormatManager.formatToLocale(pdfData?.bill?.purchaseOrder?.afterTaxAmount) || '';
                methods.bindPDFControls();
            },

            bindPDFControls: () => {
                const company = StorageManager.getCompany() || state.company;
                state.company = {
                    name: company.name,
                    emailAddress: company.emailAddress,
                    phoneNumber: company.phoneNumber,
                    street: company.street,
                    city: company.city,
                    state: company.state,
                    zipCode: company.zipCode,
                    country: company.country
                };
                state.companyAddress = [
                    company.street,
                    company.city,
                    company.state,
                    company.zipCode,
                    company.country
                ].filter(Boolean).join(', ');

                state.vendorAddress = [
                    state.vendor.street,
                    state.vendor.city,
                    state.vendor.state,
                    state.vendor.zipCode,
                    state.vendor.country
                ].filter(Boolean).join(', ');
            }
        };

        const handler = {
            downloadPDF: async () => {
                state.isDownloading = true;
                await new Promise(resolve => setTimeout(resolve, 500));

                try {
                    const { jsPDF } = window.jspdf;
                    const doc = new jsPDF('p', 'mm', 'a4');
                    const content = document.getElementById('content');

                    await html2canvas(content, {
                        scale: 2,
                        useCORS: true
                    }).then(canvas => {
                        const imgData = canvas.toDataURL('image/png');
                        const imgWidth = 210;
                        const pageHeight = 297;
                        const imgHeight = (canvas.height * imgWidth) / canvas.width;
                        let position = 0;

                        doc.addImage(imgData, 'PNG', 0, position, imgWidth, imgHeight);
                        doc.save(`payment-disburse-${state.paymentDisburseNumber || 'unknown'}.pdf`);
                    });
                } catch (error) {
                    console.error('Error generating PDF:', error);
                } finally {
                    state.isDownloading = false;
                }
            },
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['PaymentDisburses']);
                var urlParams = new URLSearchParams(window.location.search);
                var id = urlParams.get('id');
                await methods.populatePDFData(id ?? '');
            } catch (e) {
                console.error('page init error:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        return {
            state,
            handler,
        };
    }
};

Vue.createApp(App).mount('#app');