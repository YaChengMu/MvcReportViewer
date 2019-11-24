using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlanJuden.MvcReportViewer
{
    public static class ReportServiceHelpers
    {
        private static System.ServiceModel.HttpBindingBase _initializeHttpBinding(string url, ReportViewerModel model)
        {
            if (url.ToLower().StartsWith("https"))
            {
                var binding = new System.ServiceModel.BasicHttpsBinding(System.ServiceModel.BasicHttpsSecurityMode.Transport);
                binding.Security.Transport.ClientCredentialType = model.ClientCredentialType;
                binding.MaxReceivedMessageSize = int.MaxValue;
                if (model.Timeout.HasValue)
                {
                    if (model.Timeout == System.Threading.Timeout.Infinite)
                    {
                        binding.CloseTimeout = TimeSpan.MaxValue;
                        binding.OpenTimeout = TimeSpan.MaxValue;
                        binding.ReceiveTimeout = TimeSpan.MaxValue;
                        binding.SendTimeout = TimeSpan.MaxValue;
                    }
                    else
                    {
                        binding.CloseTimeout = new TimeSpan(0, 0, model.Timeout.Value);
                        binding.OpenTimeout = new TimeSpan(0, 0, model.Timeout.Value);
                        binding.ReceiveTimeout = new TimeSpan(0, 0, model.Timeout.Value);
                        binding.SendTimeout = new TimeSpan(0, 0, model.Timeout.Value);
                    }
                }

                return binding;
            }
            else
            {
                var binding = new System.ServiceModel.BasicHttpBinding(System.ServiceModel.BasicHttpSecurityMode.TransportCredentialOnly);
                binding.Security.Transport.ClientCredentialType = model.ClientCredentialType;
                binding.MaxReceivedMessageSize = int.MaxValue;
                if (model.Timeout.HasValue)
                {
                    if (model.Timeout == System.Threading.Timeout.Infinite)
                    {
                        binding.CloseTimeout = TimeSpan.MaxValue;
                        binding.OpenTimeout = TimeSpan.MaxValue;
                        binding.ReceiveTimeout = TimeSpan.MaxValue;
                        binding.SendTimeout = TimeSpan.MaxValue;
                    }
                    else
                    {
                        binding.CloseTimeout = new TimeSpan(0, 0, model.Timeout.Value);
                        binding.OpenTimeout = new TimeSpan(0, 0, model.Timeout.Value);
                        binding.ReceiveTimeout = new TimeSpan(0, 0, model.Timeout.Value);
                        binding.SendTimeout = new TimeSpan(0, 0, model.Timeout.Value);
                    }
                }

                return binding;
            }
        }

        public static async Task<ReportService.ItemParameter[]> GetReportParametersAsync(ReportViewerModel model, bool forRendering = false)
        {
            var url = model.ServerUrl + ((model.ServerUrl.ToSafeString().EndsWith("/")) ? "" : "/") + "ReportService2010.asmx";

            var basicHttpBinding = _initializeHttpBinding(url, model);
            var service = new ReportService.ReportingService2010SoapClient(basicHttpBinding, new System.ServiceModel.EndpointAddress(url));
            service.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
            service.ClientCredentials.Windows.ClientCredential = (System.Net.NetworkCredential)(model.Credentials ?? System.Net.CredentialCache.DefaultCredentials);

            string historyID = null;
            ReportService.ParameterValue[] values = null;
            ReportService.DataSourceCredentials[] rsCredentials = null;
            ReportService.TrustedUserHeader trustedUserHeader = new ReportService.TrustedUserHeader();

            
            var parameters = await service.GetItemParametersAsync(trustedUserHeader, model.ReportPath, historyID, false, values, rsCredentials);    //set it to load the not for rendering so that it's hopefully quicker than the whole regular call

            if (model != null && model.Parameters != null && model.Parameters.Any())
            {
                var tempParameters = new List<ReportService.ParameterValue>();
                foreach (var parameter in parameters.Parameters)
                {
                    if (model.Parameters.ContainsKey(parameter.Name))
                    {
                        var providedParameter = model.Parameters[parameter.Name];
                        if (providedParameter != null)
                        {
                            foreach (var value in providedParameter.Where(x => !String.IsNullOrEmpty(x)))
                            {
                                tempParameters.Add(new ReportService.ParameterValue()
                                {
                                    Label = parameter.Name,
                                    Name = parameter.Name,
                                    Value = value
                                });
                            }
                        }
                    }
                }

                values = tempParameters.ToArray();
            }

            parameters = await service.GetItemParametersAsync(trustedUserHeader,model.ReportPath, historyID, forRendering, values, rsCredentials);

            return parameters.Parameters;
        }

        public static async Task<ReportExportResult> ExportReportToFormatAsync(ReportViewerModel model, ReportFormats format, int? startPage = 0, int? endPage = 0)
        {
            return await ExportReportToFormatAsync(model, format.GetName(), startPage, endPage);
        }

        public static async Task <ReportExportResult> ExportReportToFormatAsync(ReportViewerModel model, string format, int? startPage = 0, int? endPage = 0)
        {
            var definedReportParameters = await GetReportParametersAsync(model, true);

            var url = model.ServerUrl + ((model.ServerUrl.ToSafeString().EndsWith("/")) ? "" : "/") + "ReportExecution2005.asmx";

            var basicHttpBinding = _initializeHttpBinding(url, model);
            var service = new ReportServiceExecution.ReportExecutionServiceSoapClient(basicHttpBinding, new System.ServiceModel.EndpointAddress(url));
            service.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
            service.ClientCredentials.Windows.ClientCredential = (System.Net.NetworkCredential)(model.Credentials ?? System.Net.CredentialCache.DefaultCredentials);

            var exportResult = new ReportExportResult();
            exportResult.CurrentPage = (startPage.ToInt32() <= 0 ? 1 : startPage.ToInt32());
            exportResult.SetParameters(definedReportParameters, model.Parameters);

            if (startPage == 0)
            {
                startPage = 1;
            }

            if (endPage == 0)
            {
                endPage = startPage;
            }

            var outputFormat = $"<OutputFormat>{format}</OutputFormat>";
            var encodingFormat = $"<Encoding>{model.Encoding.EncodingName}</Encoding>";
            var htmlFragment = ((format.ToUpper() == "HTML4.0" && model.UseCustomReportImagePath == false && model.ViewMode == ReportViewModes.View) ? "<HTMLFragment>true</HTMLFragment>" : "");
            var deviceInfo = $"<DeviceInfo>{outputFormat}{encodingFormat}<Toolbar>False</Toolbar>{htmlFragment}</DeviceInfo>";
            if (model.ViewMode == ReportViewModes.View && startPage.HasValue && startPage > 0)
            {
                if (model.EnablePaging)
                {
                    deviceInfo = $"<DeviceInfo>{outputFormat}<Toolbar>False</Toolbar>{htmlFragment}<Section>{startPage}</Section></DeviceInfo>";
                }
                else
                {
                    deviceInfo = $"<DeviceInfo>{outputFormat}<Toolbar>False</Toolbar>{htmlFragment}</DeviceInfo>";
                }
            }

            var reportParameters = new List<ReportServiceExecution.ParameterValue>();
            foreach (var parameter in exportResult.Parameters)
            {
                bool addedParameter = false;
                foreach (var value in parameter.SelectedValues)
                {
                    var reportParameter = new ReportServiceExecution.ParameterValue();
                    reportParameter.Name = parameter.Name;
                    reportParameter.Value = value;
                    reportParameters.Add(reportParameter);

                    addedParameter = true;
                }

                if (!addedParameter)
                {
                    var reportParameter = new ReportServiceExecution.ParameterValue();
                    reportParameter.Name = parameter.Name;
                    reportParameters.Add(reportParameter);
                }
            }

            var executionHeader = new ReportServiceExecution.ExecutionHeader();
            var trustedUserHeader = new ReportServiceExecution.TrustedUserHeader();

            ReportServiceExecution.ExecutionInfo executionInfo = null;
            string extension = null;
            string encoding = null;
            string mimeType = null;
            string[] streamIDs = null;
            ReportServiceExecution.Warning[] warnings = null;

            try
            {
                string historyID = null;
                var taskLoadReport = await service.LoadReportAsync(trustedUserHeader, model.ReportPath, historyID);
                executionInfo = taskLoadReport.executionInfo;
                executionHeader.ExecutionID = executionInfo.ExecutionID;

                ;
                var executionParameterResult =await service.SetExecutionParametersAsync(taskLoadReport.ExecutionHeader, new ReportServiceExecution.TrustedUserHeader(), reportParameters.ToArray(), "en-us");

                if (model.EnablePaging)
                {
                    var renderRequest = new ReportServiceExecution.Render2Request(executionHeader, trustedUserHeader, format, deviceInfo, ReportServiceExecution.PageCountMode.Actual);
                    var result = await service.Render2Async(executionInfo.ExecutionID, renderRequest);

                    extension = result.Extension;
                    mimeType = result.MimeType;
                    encoding = result.Encoding;
                    warnings = result.Warnings;
                    streamIDs = result.StreamIds;

                    exportResult.ReportData = result.Result;
                }
                else
                {
                    var renderRequest = new ReportServiceExecution.RenderRequest(executionHeader, trustedUserHeader, format, deviceInfo);
                    var result = await service.RenderAsync(executionInfo.ExecutionID, renderRequest);

                    extension = result.Extension;
                    mimeType = result.MimeType;
                    encoding = result.Encoding;
                    warnings = result.Warnings;
                    streamIDs = result.StreamIds;

                    exportResult.ReportData = result.Result;
                }

                executionInfo = await service.GetExecutionInfoAsync(executionHeader.ExecutionID);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            exportResult.ExecutionInfo = executionInfo;
            exportResult.Format = format;
            exportResult.MimeType = mimeType;
            exportResult.StreamIDs = (streamIDs == null ? new List<string>() : streamIDs.ToList());
            exportResult.Warnings = (warnings == null ? new List<ReportServiceExecution.Warning>() : warnings.ToList());

            if (executionInfo != null)
            {
                exportResult.TotalPages = executionInfo.NumPages;
            }

            return exportResult;
        }

        /// <summary>
        /// Searches a specific report for your provided searchText and returns the page that it located the text on.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="searchText">The text that you want to search in the report</param>
        /// <param name="startPage">Starting page for the search to begin from.</param>
        /// <returns></returns>
        public static async Task<int?> FindStringInReportAsync(ReportViewerModel model, string searchText, int? startPage = 0)
        {
            var url = model.ServerUrl + ((model.ServerUrl.ToSafeString().EndsWith("/")) ? "" : "/") + "ReportExecution2005.asmx";

            var basicHttpBinding = _initializeHttpBinding(url, model);
            var service = new ReportServiceExecution.ReportExecutionServiceSoapClient(basicHttpBinding, new System.ServiceModel.EndpointAddress(url));
            service.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
            service.ClientCredentials.Windows.ClientCredential = (System.Net.NetworkCredential)(model.Credentials ?? System.Net.CredentialCache.DefaultCredentials);

            var definedReportParameters = await GetReportParametersAsync(model, true);

            if (!startPage.HasValue || startPage == 0)
            {
                startPage = 1;
            }

            var exportResult = new ReportExportResult();
            exportResult.CurrentPage = startPage.ToInt32();
            exportResult.SetParameters(definedReportParameters, model.Parameters);

            var format = "HTML4.0";
            var outputFormat = $"<OutputFormat>{format}</OutputFormat>";
            var encodingFormat = $"<Encoding>{model.Encoding.EncodingName}</Encoding>";
            var htmlFragment = ((format.ToUpper() == "HTML4.0" && model.UseCustomReportImagePath == false && model.ViewMode == ReportViewModes.View) ? "<HTMLFragment>true</HTMLFragment>" : "");
            var deviceInfo = $"<DeviceInfo>{outputFormat}{encodingFormat}<Toolbar>False</Toolbar>{htmlFragment}</DeviceInfo>";
            if (model.ViewMode == ReportViewModes.View && startPage.HasValue && startPage > 0)
            {
                deviceInfo = $"<DeviceInfo>{outputFormat}<Toolbar>False</Toolbar>{htmlFragment}<Section>{startPage}</Section></DeviceInfo>";
            }

            var reportParameters = new List<ReportServiceExecution.ParameterValue>();
            foreach (var parameter in exportResult.Parameters)
            {
                bool addedParameter = false;
                foreach (var value in parameter.SelectedValues)
                {
                    var reportParameter = new ReportServiceExecution.ParameterValue();
                    reportParameter.Name = parameter.Name;
                    reportParameter.Value = value;
                    reportParameters.Add(reportParameter);

                    addedParameter = true;
                }

                if (!addedParameter)
                {
                    var reportParameter = new ReportServiceExecution.ParameterValue();
                    reportParameter.Name = parameter.Name;
                    reportParameters.Add(reportParameter);
                }
            }

            var executionHeader = new ReportServiceExecution.ExecutionHeader();
            var trustedUserHeader = new ReportServiceExecution.TrustedUserHeader();

            ReportServiceExecution.ExecutionInfo executionInfo = null;
            string extension = null;
            string encoding = null;
            string mimeType = null;
            string[] streamIDs = null;
            ReportServiceExecution.Warning[] warnings = null;

            try
            {
                string historyID = null;
                var taskLoadReport = await service.LoadReportAsync(trustedUserHeader, model.ReportPath, historyID);
                executionHeader = taskLoadReport.ExecutionHeader;
                executionInfo = taskLoadReport.executionInfo;
                var executionParameterResult = await service.SetReportParametersAsync(executionInfo.ExecutionID, reportParameters.ToArray(), "en-us");

                var renderRequest = new ReportServiceExecution.Render2Request(executionHeader, trustedUserHeader, format, deviceInfo, ReportServiceExecution.PageCountMode.Actual);
                var result = await service.Render2Async(executionInfo.ExecutionID, renderRequest);

                extension = result.Extension;
                mimeType = result.MimeType;
                encoding = result.Encoding;
                warnings = result.Warnings;
                streamIDs = result.StreamIds;

                executionInfo = await service.GetExecutionInfoAsync(executionHeader.ExecutionID);

                return await service.FindStringAsync(executionInfo.ExecutionID, startPage.ToInt32(), executionInfo.NumPages, searchText);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return 0;
        }

        /// <summary>
        /// I'm using this method to run images through a "proxy" on the local site due to credentials used on the report being different than the currently running user.
        /// I ran into issues where my domain account was different than the user that executed the report so the images gave 500 errors from the website. Also my report server
        /// is only internally available so this solved that issue for me as well.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="reportContent">This is the raw html output of your report.</param>
        /// <returns></returns>
        public static string ReplaceImageUrls(ReportViewerModel model, string reportContent)
        {
            var reportServerDomainUri = new Uri(model.ServerUrl);
            var searchForUrl = $"SRC=\"{reportServerDomainUri.Scheme}://{reportServerDomainUri.DnsSafeHost}/";
            //replace image urls with image data instead due to having issues accessing the images as a different authenticated user
            var imagePathIndex = reportContent.IndexOf(searchForUrl);
            while (imagePathIndex > -1)
            {
                var endIndex = reportContent.IndexOf("\"", imagePathIndex + 5);   //account for the length of src="
                if (endIndex > -1)
                {
                    var imageUrl = reportContent.Substring(imagePathIndex + 5, endIndex - (imagePathIndex + 5));
                    reportContent = reportContent.Replace(imageUrl, $"{String.Format(model.ReportImagePath, imageUrl)}");
                }

                imagePathIndex = reportContent.IndexOf(searchForUrl, imagePathIndex + 5);
            }

            return reportContent;
        }
    }
}
