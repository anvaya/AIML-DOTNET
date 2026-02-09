# AgenticAI - Intelligent File System Agent

A sophisticated .NET console application demonstrating the power of **Agentic AI** using Microsoft Semantic Kernel and OpenAI GPT-4o. This project showcases an AI agent capable of interacting with the file system, handling errors gracefully, and solving complex problems autonomously.

## 🎯 Project Overview

This is a learning/tutorial project that demonstrates:
- How to build an AI agent using Microsoft Semantic Kernel
- Integration with OpenAI's GPT-4o model
- Creating custom kernel plugins for file system operations
- Advanced error handling and problem-solving capabilities
- Windows Restart Manager API integration for locked file detection

## ✨ Key Features

- **Semantic Kernel Integration**: Leverages Microsoft's Semantic Kernel framework for AI orchestration
- **Custom Plugins**: Implements a FileSystemTools plugin with file deletion and process management capabilities
- **Intelligent Error Handling**: The agent can detect, diagnose, and recover from file access errors
- **Windows Restart Manager API**: Uses native Windows APIs to detect processes locking files
- **Interactive Chat**: Maintains conversation history for context-aware interactions

## 📋 Prerequisites

- **.NET 9.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Visual Studio 2022** (recommended) or any C# IDE
- **OpenAI API Key** - See instructions below
- **Windows OS** (required for Restart Manager API)

## 🚀 Getting Started

### Step 1: Clone or Download the Project

```bash
git clone <repository-url>
cd AgenticAI
```

### Step 2: Obtain Your OpenAI API Key

To use this application, you'll need an OpenAI API key. Follow these steps:

1. **Create an OpenAI Account**
   - Visit [https://platform.openai.com](https://platform.openai.com)
   - Sign up for a new account or log in if you already have one

2. **Access the API Dashboard**
   - After logging in, navigate to the API section
   - Click on your profile name in the top right corner
   - Select "API keys" or "View API keys" from the menu

3. **Generate a New API Key**
   - Click the "Create new secret key" button
   - Give your key a descriptive name (optional but recommended)
   - **Copy the generated key immediately** - you won't be able to see it again!

4. **Set Up Billing**
   - New accounts typically receive **$5 in free credits** to start
   - You'll need to add a payment method for continued use
   - Check the [OpenAI pricing page](https://openai.com/pricing) for current rates

> **⚠️ Security Warning**: Never commit your API key to version control or share it publicly!

### Step 3: Configure Your API Key

1. Open the `App.config` file in the `AgenticAI` project
2. Locate the `<appSettings>` section
3. Replace `YOUR_OPENAI_API_KEY_HERE` with your actual API key:

```xml
<appSettings>
    <add key="OpenAIApiKey" value="sk-proj-YOUR_ACTUAL_API_KEY_HERE" />
</appSettings>
```

4. Save the file

### Step 4: Restore Dependencies

```bash
dotnet restore
```

Or in Visual Studio:
- Right-click the solution in Solution Explorer
- Select "Restore NuGet Packages"

### Step 5: Build and Run

**Using Visual Studio:**
- Press `F5` or click the "Start" button
- The application will launch in a console window

**Using Command Line:**
```bash
dotnet build
dotnet run
```

## 📁 Project Structure

```
AgenticAI/
├── AgenticAI/
│   ├── Program.cs           # Main entry point and AI agent logic
│   ├── RestartManager.cs    # Windows Restart Manager API wrapper
│   ├── App.config           # Configuration file (add your API key here)
│   └── AgenticAI.csproj     # Project configuration
├── AgenticAI.sln            # Visual Studio solution file
└── README.md                # This file
```

## 🔧 Key Components Explained

### 1. Program.cs
The heart of the application that:
- Initializes the Semantic Kernel with OpenAI configuration
- Creates and registers the FileSystemTools plugin
- Sets up chat history with system instructions
- Demonstrates file deletion with error handling

### 2. RestartManager.cs
A utility class that wraps the Windows Restart Manager API:
- Detects which processes are holding locks on specific files
- Uses P/Invoke to call native Windows functions (`rstrtmgr.dll`)
- Provides detailed information about locking applications

### 3. FileSystemTools Plugin
A custom Semantic Kernel plugin with two functions:
- **DeleteFile**: Attempts to delete a file with comprehensive error handling
- **EndProcess**: Terminates a process by name (use with caution!)

## 💡 How It Works

1. The application creates a Semantic Kernel instance configured for OpenAI
2. The FileSystemTools plugin is registered with the kernel
3. The AI is given instructions on how to handle file operations
4. When a file deletion is requested, the agent:
   - Attempts to delete the file
   - If it fails, analyzes the error
   - Uses the RestartManager to identify blocking processes
   - Attempts to terminate those processes
   - Retries the deletion

## 🔐 Security Notes

- The `App.config` file is excluded from Git via `.gitignore` to prevent accidental API key exposure
- Never share your API key or commit it to version control
- Consider using environment variables or Azure Key Vault for production applications

## 📚 Dependencies

This project uses the following NuGet packages:
- **Microsoft.SemanticKernel.Abstractions** (v1.70.0)
- **Microsoft.SemanticKernel.Connectors.OpenAI** (v1.70.0)
- **System.Configuration.ConfigurationManager**

## 🎓 Learning Objectives

This project demonstrates:
- Building AI agents with Semantic Kernel
- Creating custom kernel plugins
- Integrating with OpenAI's GPT models
- Windows API interop (P/Invoke)
- Error handling in AI applications
- Agent problem-solving capabilities

## 📖 Additional Resources

- [Microsoft Semantic Kernel Documentation](https://learn.microsoft.com/en-us/semantic-kernel/)
- [OpenAI API Documentation](https://platform.openai.com/docs)
- [.NET 9.0 Documentation](https://learn.microsoft.com/en-us/dotnet/core/)

## 🐛 Troubleshooting

### Common Issues

**"API key not found" error:**
- Ensure you've added your API key to `App.config`
- Check that the key format is correct (starts with `sk-`)

**Build errors:**
- Make sure you have .NET 9.0 SDK installed
- Run `dotnet restore` to restore dependencies

**Runtime errors:**
- Verify your OpenAI account has available credits
- Check that the API key is valid and active

## 📝 License

This is a learning/tutorial project. Feel free to use it for educational purposes.

## 🤝 Contributing

This is a learning project, but suggestions and improvements are welcome!

---

**Happy Learning!** If you find this project helpful, consider exploring more about Agentic AI and the Semantic Kernel framework.
