CREATE TABLE "processed_manifests"(
									  "Id" INTEGER NOT NULL,
									  "ProcessedDateTime" TEXT NOT NULL,
									  "OriginalFilename" TEXT NOT NULL,
									  "ManifestDate" TEXT NOT NULL,
									  "TotalCrates" INTEGER NOT NULL,
									  "Status" INTEGER NOT NULL,
									  "LastError" TEXT NOT NULL,
									  "ReceiptId" TEXT NOT NULL,
									  "TotalShipments" INTEGER NOT NULL,
									  "ReceiptXml" TEXT NOT NULL,
									  "ShipmentXml" TEXT NOT NULL,
									  "Delivered" INTEGER NOT NULL,
									  PRIMARY KEY ("Id")
)
