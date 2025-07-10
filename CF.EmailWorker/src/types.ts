
type DotnetOrder = {
	orderDate: string;
	storeNumber: string;
	storeName: string;
	poNumber: string;
	customerNumber: string;
	orderNumber: string;
	inventoryNumber: string;
	quantity: number;
	crateQuantity: number;
}

type DotnetManifest = {
	orders: DotnetOrder[];
	totalOrders: number;
	totalCrates: number;
	manifestDate: string;
}

type ApiResponse = {
	message: string;
	error: string
	originalFilename?: string;
	manifestDate?: string;
	totalOrders?: number;
	totalCrates?: number;
	company?: string;
	manifest?: DotnetManifest;
	receiptXmlContent?: string;
	shipmentXmlContent?: string;
};

type EmailAttachment = {
	filename: string;
	contentType: string;
	fileType: string; // 'pdf', 'csv', 'xlsx', etc.
	data: ArrayBuffer | string; // Use ArrayBuffer for binary data
};

type EmailProcessingResponse = {
	message: string;
	status: number;
}

export { DotnetOrder, DotnetManifest, ApiResponse, EmailAttachment, EmailProcessingResponse };
