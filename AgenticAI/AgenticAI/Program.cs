using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Configuration;

namespace AgenticAI
{
    internal class Program
    {
        static async Task Main(string[] args)
        {            
            string delete_file_path = System.IO.Path.GetTempFileName();

            var builder = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion("gpt-4o", ConfigurationManager.AppSettings["OPENAI_API_KEY"]
                            ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set."));

            builder.Plugins.AddFromType<FileSystemTools>();

            var kernel = builder.Build();
            
            string template = "You are an intelligent agent that can perform file system operations using available plugins and tools." +
                "When given a task, you should determine which tool(s) to use and in what order to achieve the desired outcome. " +
                "If a tool returns an error, you should analyze the error message and decide how to proceed, whether it's retrying the tool, using a different tool, or providing a helpful error message to the user. " +
                "Always aim to complete the task successfully while minimizing errors and retries. ";

            
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();           

            // Initialize chat history with prompt.
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(template);
            chatHistory.AddUserMessage($"I want to delete file {delete_file_path}");

            // Initialize execution settings with enabled auto function calling.
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            // Set chat history in kernel data to access it in a function.
            kernel.Data[nameof(ChatHistory)] = chatHistory;  // Future use.

            // Send a request.
            var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory, executionSettings, kernel);
            chatHistory.Add(result);

            Console.WriteLine(result.Content);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

    }


    #region private

    class ToolResponse
    {
        /// <summary>
        /// This variable will contain the result of the tool execution. 
        /// If the tool execution was successful, it will have value = "success", otherwise it will have value = "error" and the ErrorMessage variable will contain the error message.
        /// </summary>
        public string Result { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the error message associated with the current context.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets informational text. This can be useful in sucessful tool execution to provide additional information about the execution result,         
        /// </summary>
        public string Information { get; set; } = string.Empty;
    }


    sealed class FileSystemTools
    {
#if DEBUG
        static bool file_lock_simultated = false;
#endif

        [KernelFunction]
        [Description("Deletes a file at the specified path.If the file is locked by another process, it will throw an error with the message containing the process id and process name that is locking the file.")]
        public static ToolResponse DeleteFile(Kernel kernel, KernelArguments arguments, string filePath)
        {            
            try
            {
#if DEBUG
                //Let's simulate a locked file scenario for testing purposes. The first time the function is called, it will return a locked file error,
                //and on subsequent calls, it will allow the file to be deleted successfully.
                //This will help us test the agent's ability to handle locked files and retry logic.
                if (!file_lock_simultated)
                {
                    file_lock_simultated = true;
                    return new ToolResponse { Result = "error", ErrorMessage = $"File is locked by process(es): id:5656 name:'Testing Process'" };
                }
#endif
                // Check if the file is locked by another process and throw an error with process id
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);                   
                    return new ToolResponse { Result = "success", Information = $"File deleted successfully: {filePath}" };
                }
                else
                {                                       
                    return new ToolResponse { Result = "error", ErrorMessage = $"File not found: {filePath}" };
                }
            }
            catch (IOException ex)
            {
                // Check if the exception is due to a sharing violation (locked file)
                // HResult 0x80070020 corresponds to ERROR_SHARING_VIOLATION
                // HResult 0x80070005 corresponds to UnauthorizedAccessException (permissions)
                int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(ex) & 0x0000FFFF;
                if (errorCode == 32 || errorCode == 5)
                {
                    // File is locked, wait and retry
                    var infos = RestartManager.GetProcessesUsingFiles(new List<string> { filePath }).FirstOrDefault();
                    if (infos != null)
                    {                        
                        return new ToolResponse { Result = "error", ErrorMessage = $"File is locked by process(es): id:{infos.Id} name:{infos.ProcessName}" };                     
                    }
                    else
                    {                        
                        return new ToolResponse { Result = "error", ErrorMessage = $"File is locked by an unknown process." };                        
                    }
                }
                else
                {                                        
                    return new ToolResponse { Result = "error", ErrorMessage = $"IO Exception: {ex.Message}" };
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Lacks permission, wait and retry for transient issues               
                return new ToolResponse { Result = "error", ErrorMessage = $"Unauthorized Access: You do not have permission to delete the file: {filePath}" };

            }
            catch (Exception)
            {
                // Handle any other unexpected exceptions                
                return new ToolResponse { Result = "error", ErrorMessage = $"An unexpected error occurred while trying to delete the file: {filePath}" };
            }
        }

        [KernelFunction]
        [Description("Ends a process with the specified process id and process name. This can be useful if a file is locked by a process, you can terminate the process and then re-attempt the delete file.")]
        public static ToolResponse EndProcess(Kernel kernel, KernelArguments arguments, string ProcessId, string ProcessName)
        {
            Console.WriteLine($"Attempting to end process with ID: {ProcessId} Name:{ProcessName}");
#if !DEBUG
            try
            { 
                System.Diagnostics.Process.GetProcessById(int.Parse(ProcessId)).Kill();                
                return new ToolResponse { Result = "success", Information = $"Process with ID: {ProcessId} Name:{ProcessName} has been terminated." };
            }catch(Exception ex)
            {
                return new ToolResponse { Result = "error", ErrorMessage = $"Failed to terminate process with ID: {ProcessId} Name:{ProcessName}. Exception: {ex.Message}" };
            }
#else
            return new ToolResponse { Result = "success", Information = $"Process with ID: {ProcessId} Name:{ProcessName} has been terminated." };
#endif
        }
    }
#endregion
}
