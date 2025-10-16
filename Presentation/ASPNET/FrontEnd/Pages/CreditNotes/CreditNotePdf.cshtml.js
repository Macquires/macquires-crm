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
            salesReturnNumber: '',
            customerAddress: '',
            creditNoteNumber: '',
            creditNoteDate: '',
            creditNoteCurrency: '',
            subTotal: '',
            tax: '',
            totalAmount: '',
            items: [],
            isDownloading: false
        });

        const services = {
            getPDFData: async (id) => {
                try {
                    const response = await AxiosManager.get('/CreditNote/GetCreditNoteSingle?id=' + id, {});
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
                
                state.salesReturnNumber = pdfData?.salesReturn?.number;
                state.items = response?.data?.content?.transactionList?.map(item => ({
                    ...item,
                    total: item.product.unitPrice * item.movement
                })) || [];
                state.customer = pdfData?.salesReturn?.deliveryOrder?.salesOrder?.customer || {};
                state.creditNoteNumber = pdfData.number || '';
                state.creditNoteDate = DateFormatManager.formatToLocale(pdfData.creditNoteDate) || '';
                state.creditNoteCurrency = StorageManager.getCompany()?.currency || '';
                state.subTotal = NumberFormatManager.formatToLocale(response?.data?.content?.beforeTaxAmount) || '';
                state.tax = NumberFormatManager.formatToLocale(response?.data?.content?.taxAmount) || '';
                state.totalAmount = NumberFormatManager.formatToLocale(response?.data?.content?.afterTaxAmount) || '';
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
                        doc.save(`credit-note-${state.creditNoteNumber || 'unknown'}.pdf`);
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
                await SecurityManager.authorizePage(['CreditNotes']);
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