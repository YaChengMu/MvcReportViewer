using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ServiceModel;

namespace ReportServiceExecution
{
    public partial class ReportExecutionServiceSoapClient
    {
		public async Task<int> FindStringAsync(string executionID, int startPage, int endPage, string findValue)
		{
			//using (OperationContextScope context = SetMessageHeaders(executionID))
			//{
                var result =await this.FindStringAsync(new ExecutionHeader() { ExecutionID = executionID }, new TrustedUserHeader(), startPage, endPage, findValue);

                return result.PageNumber;
			//}
		}

		public async Task<ExecutionInfo> GetExecutionInfoAsync(string executionID)
		{
			//using (OperationContextScope context = SetMessageHeaders(executionID))
			//{
                var result = await this.GetExecutionInfoAsync(new ExecutionHeader() { ExecutionID = executionID }, new TrustedUserHeader());

                return result.executionInfo;
			//}
		}
		public async Task<RenderResponse> RenderAsync(string executionID, ReportServiceExecution.RenderRequest request)
		{
			//using (OperationContextScope context = SetMessageHeaders(executionID))
			//{
				return await this.RenderAsync(request);
			//}
		}

		public async Task<Render2Response> Render2Async(string executionID, ReportServiceExecution.Render2Request request)
		{
			//using (OperationContextScope context = SetMessageHeaders(executionID))
			//{
				return await this.Render2Async(request);
			//}
		}

		public async Task<ExecutionInfo> SetReportParametersAsync(string executionID, IEnumerable<ParameterValue> parameterValues, string parameterLanguage)
		{
			//using (OperationContextScope context = SetMessageHeaders(executionID))
			//{
				ParameterValue[] parameterValuesArray = parameterValues.ToArray();
				if (parameterLanguage == null || parameterLanguage == "")
				{
					parameterLanguage = System.Globalization.CultureInfo.CurrentUICulture.Name;
				}

                var result = await this.SetExecutionParametersAsync(new ExecutionHeader() { ExecutionID = executionID }, new TrustedUserHeader(), parameterValuesArray, parameterLanguage);


                return result.executionInfo;
			//}
		}

		private OperationContextScope SetMessageHeaders(string executionID)
		{
			OperationContextScope context = new OperationContextScope(this.InnerChannel);

			ExecutionHeader executionHeaderData = new ExecutionHeader()
			{
				ExecutionID = executionID,
				//ExecutionIDForWcfSoapHeader = executionID
			};

#if true
			// add the ExecutionHeader entry to the soap headers
			//OperationContext.Current.OutgoingMessageHeaders.Add(executionHeaderData.CreateMessageHeader());
#else
				// this does not appear to affect the soap headers
				OperationContext.Current.OutgoingMessageProperties.Add(ExecutionHeader.HeaderName, executionHeaderData);
#endif

			return context;
		}
	}
}
