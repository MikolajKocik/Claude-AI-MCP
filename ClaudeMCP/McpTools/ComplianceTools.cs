using ClaudeMCP.Clients;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ClaudeMCP.McpTools;

[McpServerToolType]
public sealed class ComplianceTools
{
    private readonly ClaudeClient _client;
    private readonly ILogger<ComplianceTools> _logger;

    public ComplianceTools(ClaudeClient client, ILogger<ComplianceTools> logger)
    {
        _client = client;
        _logger = logger;   
    }

    /// <summary>
    /// Analyzes the provided document content for compliance with specified standards.
    /// </summary>
    /// <remarks>The analysis includes a summary of risks, vulnerabilities, and recommendations, as well as a
    /// prioritized "What to fix first" section.</remarks>
    /// <param name="documentContent">The content of the document to be analyzed. This parameter cannot be null or empty.</param>
    /// <param name="standard">The compliance standard to evaluate against (e.g., "RODO", "ISO 27001", "SOC 2"). If null or empty, the default
    /// standards "RODO, ISO 27001, and SOC 2" will be used.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is a string containing the compliance
    /// analysis, including identified risks, vulnerabilities, consequences, and remediation recommendations.</returns>
    [McpServerTool, Description("Analyze the document for compliance with RODO/ISO/SOC2")]
    public async Task<string> AnalyzeComplianceAsync(string documentContent, CancellationToken ct, string? standard = null)
    {
        var s = string.IsNullOrWhiteSpace(standard) ? "RODO, ISO 27001 i SOC 2" : standard;

        string prompt =
            $@"You are a compliance auditor. Evaluate the following material against: {s}.
            Identify: (1) risks and vulnerabilities, (2) consequences, (3) remediation recommendations.
            Provide the result as a concise list + a 'What to fix first' section.

            ============= DOCUMENT =============
            {documentContent}";

        _logger.LogInformation("Sending document to Claude...");
        return await _client.AskClaudeAsync(prompt, ct);
    }

    /// <summary>
    /// Generates a compliance audit report based on the provided findings, scope, and timeframe.
    /// </summary>
    /// <remarks>The generated report includes sections such as an executive summary, methodology, scope, 
    /// results, risks, recommendations, priorities, and attachments. The findings are synthesized  into a professional
    /// and actionable format, suitable for compliance purposes.</remarks>
    /// <param name="findingsJsonOrText">The raw findings to include in the report, provided as a JSON string or plain text.</param>
    /// <param name="scope">The scope of the audit, describing the areas or topics covered by the report.</param>
    /// <param name="timeframe">The timeframe for the audit, specifying the period under review.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the generated audit report as a
    /// string.</returns>
    [McpServerTool, Description("Generates an audit report based on tool findings")]
    public async Task<string> GenerateAuditReportAsync(string findingsJsonOrText, string scope, string timeframe, CancellationToken ct)
    {
        string prompt =
            $@"Synthesize a professional compliance audit report (Executive Summary, Methodology, Scope: {scope}, Period: {timeframe},
            Results, Risks, Recommendations, Priorities, Attachments). Here are the raw findings:

            {findingsJsonOrText}

            Ensure a clear structure and checklists for immediate implementation.";
        return await _client.AskClaudeAsync(prompt, ct, temperature: 0.1, maxTokens: 3000);
    }
}
