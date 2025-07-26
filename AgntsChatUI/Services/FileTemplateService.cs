namespace AgntsChatUI.Services
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of IFileTemplateService for managing agent template files
    /// </summary>
    public class FileTemplateService : IFileTemplateService
    {
        private readonly string _baseDirectory;

        public FileTemplateService()
        {
            this._baseDirectory = this.ResolvePromptTemplatesDirectory();
        }

        /// <summary>
        /// Resolves the path to the PromptTemplates directory using multiple strategies
        /// </summary>
        /// <returns>The absolute path to the PromptTemplates directory</returns>
        private string ResolvePromptTemplatesDirectory()
        {
            // Strategy 1: Look for AgntsChatUI.AI in the same directory as the current executable
            string currentDir = Directory.GetCurrentDirectory();
            string sameLevelPath = Path.Combine(currentDir, "..", "AgntsChatUI.AI", "PromptTemplates");
            string normalizedSameLevelPath = Path.GetFullPath(sameLevelPath);

            if (Directory.Exists(normalizedSameLevelPath))
            {
                return normalizedSameLevelPath;
            }

            // Strategy 2: Look for AgntsChatUI.AI in the solution directory (3 levels up)
            string? solutionDir = Directory.GetParent(currentDir)?.Parent?.Parent?.FullName;
            if (!string.IsNullOrEmpty(solutionDir))
            {
                string solutionPath = Path.Combine(solutionDir, "AgntsChatUI.AI", "PromptTemplates");
                if (Directory.Exists(solutionPath))
                {
                    return solutionPath;
                }
            }

            // Strategy 3: Look for AgntsChatUI.AI in parent directories (up to 5 levels)
            string searchDir = currentDir;
            for (int i = 0; i < 5; i++)
            {
                DirectoryInfo? parentDir = Directory.GetParent(searchDir);
                if (parentDir == null)
                {
                    break;
                }

                string candidatePath = Path.Combine(parentDir.FullName, "AgntsChatUI.AI", "PromptTemplates");
                if (Directory.Exists(candidatePath))
                {
                    return candidatePath;
                }

                searchDir = parentDir.FullName;
            }

            // Strategy 4: Look for any directory named "PromptTemplates" in the current directory tree
            string? promptTemplatesDir = this.FindDirectoryRecursively(currentDir, "PromptTemplates", maxDepth: 3);
            if (!string.IsNullOrEmpty(promptTemplatesDir))
            {
                return promptTemplatesDir;
            }

            // Strategy 5: Fallback - create in current directory
            string fallbackPath = Path.Combine(currentDir, "PromptTemplates");
            return fallbackPath;
        }

        /// <summary>
        /// Gets the resolved base directory path (for debugging purposes)
        /// </summary>
        /// <returns>The absolute path to the PromptTemplates directory</returns>
        public string GetBaseDirectory() => this._baseDirectory;

        /// <summary>
        /// Recursively searches for a directory with the specified name
        /// </summary>
        /// <param name="startDirectory">The directory to start searching from</param>
        /// <param name="targetDirectoryName">The name of the directory to find</param>
        /// <param name="maxDepth">Maximum depth to search</param>
        /// <param name="currentDepth">Current search depth</param>
        /// <returns>The full path to the found directory, or null if not found</returns>
        private string? FindDirectoryRecursively(string startDirectory, string targetDirectoryName, int maxDepth, int currentDepth = 0)
        {
            if (currentDepth > maxDepth || !Directory.Exists(startDirectory))
            {
                return null;
            }

            try
            {
                // Check if the target directory exists in the current directory
                string targetPath = Path.Combine(startDirectory, targetDirectoryName);
                if (Directory.Exists(targetPath))
                {
                    return targetPath;
                }

                // Search subdirectories
                foreach (string subDir in Directory.GetDirectories(startDirectory))
                {
                    string? result = this.FindDirectoryRecursively(subDir, targetDirectoryName, maxDepth, currentDepth + 1);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we don't have access to
            }
            catch (Exception)
            {
                // Skip directories that cause other errors
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task EnsureDirectoriesExistAsync()
        {
            try
            {
                string instructionsDir = Path.Combine(this._baseDirectory, "Instructions");
                string personasDir = Path.Combine(this._baseDirectory, "Personas");

                if (!Directory.Exists(instructionsDir))
                {
                    Directory.CreateDirectory(instructionsDir);
                }

                if (!Directory.Exists(personasDir))
                {
                    Directory.CreateDirectory(personasDir);
                }

                await Task.CompletedTask;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException($"Access denied when creating directories in '{this._baseDirectory}'. Please check directory permissions.", ex);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"IO error when creating directories in '{this._baseDirectory}'. The path may be invalid or the disk may be full.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error when creating directories in '{this._baseDirectory}': {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<string> CreateInstructionFileAsync(string agentName, string description)
        {
            await this.EnsureDirectoriesExistAsync();

            string fileName = $"{this.SanitizeFileName(agentName)}.md";
            string filePath = Path.Combine(this._baseDirectory, "Instructions", fileName);

            try
            {
                string content = this.GenerateInstructionContent(agentName, description);
                await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
                return filePath;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException($"Access denied when creating instruction file '{fileName}'. Please check file permissions.", ex);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"IO error when creating instruction file '{fileName}'. The file may be in use or the disk may be full.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error when creating instruction file '{fileName}': {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<string> CreatePersonaFileAsync(string agentName, string description)
        {
            await this.EnsureDirectoriesExistAsync();

            string fileName = $"{this.SanitizeFileName(agentName)}.prompty";
            string filePath = Path.Combine(this._baseDirectory, "Personas", fileName);

            try
            {
                string content = this.GeneratePersonaContent(agentName, description);
                await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
                return filePath;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException($"Access denied when creating persona file '{fileName}'. Please check file permissions.", ex);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"IO error when creating persona file '{fileName}'. The file may be in use or the disk may be full.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error when creating persona file '{fileName}': {ex.Message}", ex);
            }
        }

        private string GenerateInstructionContent(string agentName, string description)
        {
            return $@"# {agentName} Instructions

{description}

## Purpose
This agent is designed to {description.ToLower()}.

## Guidelines
- Follow the agent's specific purpose and capabilities
- Maintain consistency with the defined persona
- Provide helpful and accurate responses
- Use appropriate language and tone for the context

## Capabilities
- {description}
- Respond to user queries within the agent's scope
- Maintain context throughout conversations

## Limitations
- Stay within the defined scope and purpose
- Do not perform tasks outside of the agent's capabilities
- Refer users to appropriate resources when needed";
        }

        private string GeneratePersonaContent(string agentName, string description)
        {
            string sanitizedName = this.SanitizeFileName(agentName);

            return $@"---
name: {sanitizedName}
description: {description}
authors:
  - System Generated
model:
  api: chat
  configuration:
    type: azure_openai
    azure_deployment: ""gpt-4o""
    azure_endpoint: ${{env:AOAI_ENDPOINT}}
    api_key: ${{env:AOAI_API_KEY}}
  parameters:
    max_tokens: 4000
sample: |
  User: Hello, I need help with {description.ToLower()}.
  Assistant: I'm here to help you with {description.ToLower()}. What specific assistance do you need?
---

system:
# {agentName}

{description}

## Agent Context
### Purpose
This agent is designed to {description.ToLower()}.

### Capabilities
- {description}
- Provide helpful and accurate responses
- Maintain context throughout conversations
- Follow established guidelines and best practices

### Guidelines
- Stay focused on the agent's specific purpose
- Use appropriate language and tone
- Provide clear and actionable responses
- Ask clarifying questions when needed

### Limitations
- Stay within the defined scope and purpose
- Do not perform tasks outside of the agent's capabilities
- Refer users to appropriate resources when needed

## Response Format
Provide clear, helpful responses that align with the agent's purpose and capabilities. Use appropriate formatting and structure to make information easy to understand.";
        }

        private string SanitizeFileName(string fileName)
        {
            // Remove or replace invalid characters for file names
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = fileName;

            foreach (char invalidChar in invalidChars)
            {
                sanitized = sanitized.Replace(invalidChar, '_');
            }

            // Replace spaces with underscores
            sanitized = sanitized.Replace(' ', '_');

            // Remove multiple consecutive underscores
            while (sanitized.Contains("__"))
            {
                sanitized = sanitized.Replace("__", "_");
            }

            // Trim underscores from start and end
            sanitized = sanitized.Trim('_');

            return sanitized;
        }
    }
}