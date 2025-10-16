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
            customer: {
                name: '',
                street: '',
                city: '',
                state: '',
                zipCode: '',
                country: '',
                emailAddress: '',
                phoneNumber: ''
            },
            invoiceNumber: '',
            customerAddress: '',
            paymentReceiveNumber: '',
            paymentReceiveDate: '',
            paymentReceiveCurrency: '',
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
                    const response = await AxiosManager.get('/PaymentReceive/GetPaymentReceiveSingle?id=' + id, {});
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

                state.invoiceNumber = pdfData?.invoice?.number;
                state.paymentMethod = pdfData?.paymentMethod?.name;
                state.paymentAmount = NumberFormatManager.formatToLocale(pdfData?.paymentAmount) || '';
                state.items = pdfData?.invoice?.salesOrder?.salesOrderItemList;
                state.customer = pdfData?.invoice?.salesOrder?.customer || {};
                state.paymentReceiveNumber = pdfData.number || '';
                state.paymentReceiveDate = DateFormatManager.formatToLocale(pdfData.paymentDate) || '';
                state.paymentReceiveCurrency = StorageManager.getCompany()?.currency || '';
                state.subTotal = NumberFormatManager.formatToLocale(pdfData?.invoice?.salesOrder?.beforeTaxAmount) || '';
                state.tax = NumberFormatManager.formatToLocale(pdfData?.invoice?.salesOrder?.taxAmount) || '';
                state.totalAmount = NumberFormatManager.formatToLocale(pdfData?.invoice?.salesOrder?.afterTaxAmount) || '';
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

                state.customerAddress = [
                    state.customer.street,
                    state.customer.city,
                    state.customer.state,
                    state.customer.zipCode,
                    state.customer.country
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
                        doc.save(`payment-receive-${state.paymentReceiveNumber || 'unknown'}.pdf`);
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
                await SecurityManager.authorizePage(['PaymentReceives']);
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