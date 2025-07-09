# Manifest Processing System - Manifest To Scale

A comprehensive solution for converting Fresh To Go and Azura Fresh manifests into Manhattan Scale-compatible XML files for warehouse management systems.

## 🏗️ Architecture

This solution consists of multiple components working together to provide a complete manifest processing pipeline:

### Core Components

- **FTG.Core** - Shared library containing PDF processing, CSV handling, and XML generation logic
- **FTG.API** - RESTful API service for processing manifest files via HTTP endpoints
- **FTG.MAUI** - Cross-platform desktop application with intuitive file processing interface
- **FTG.Updater** - Auto-updater application for seamless version management

### Cloud Infrastructure

- **CF.ContainerWorker** - Cloudflare Container Worker hosting the API service
- **CF.EmailWorker** - Email processing worker for automated manifest handling via email attachments

## 🚀 Features

- **Multi-format Support**: Process PDF manifests, CSV files, and Excel spreadsheets
- **Scale Integration**: Generate Manhattan Scale-compatible RCXML (receipts) and SHXML (shipments) files
- **Email Automation**: Receive manifest files via email and automatically process them
- **Desktop Application**: User-friendly interface for local file processing
- **Cloud Processing**: Scalable API for integration with existing systems
- **Auto-updates**: Seamless application updates via integrated updater

## 📋 Supported Formats

### Input
- Fresh To Go PDF manifests
- Azura Fresh CSV files
- Excel spreadsheets (.xlsx)

### Output
- Manhattan Scale Receipt XML (.rcxml)
- Manhattan Scale Shipment XML (.shxml)
- Processing logs and reports

## 🛠️ Getting Started

Each component includes detailed setup instructions in their respective directories:

- [`FTG.MAUI/`](./FTG.MAUI/) - Desktop application
- [`FTG.API/`](./FTG.API/) - API service
- [`CF.ContainerWorker/`](./CF.ContainerWorker/) - Cloudflare container deployment
- [`CF.EmailWorker/`](./CF.EmailWorker/) - Email processing worker

## 🏢 Use Cases

- **Warehouse Management**: Automate scale system data imports
- **Supply Chain Integration**: Process vendor manifests into standardized formats
- **Email-based Processing**: Handle manifest files sent via email automatically
- **Batch Processing**: Process multiple manifest files efficiently

## 🔧 Technology Stack

- **.NET 9.0** - Core framework
- **ASP.NET Core** - API development
- **.NET MAUI** - Cross-platform desktop UI
- **Cloudflare Workers** - Serverless cloud hosting
- **iText7** - PDF processing
- **EPPlus** - Excel file handling

## 📁 Project Structure

```
├── FTG.Core/           # Shared business logic and utilities
├── FTG.API/            # REST API service
├── FTG.MAUI/           # Desktop application
├── FTG.Updater/        # Auto-updater application
├── CF.ContainerWorker/ # Cloudflare container deployment
└── CF.EmailWorker/     # Email processing service
```

## 📄 License

This project is developed for internal use and warehouse management automation.

---

For detailed setup instructions and component-specific documentation, please refer to the README files in each project directory.