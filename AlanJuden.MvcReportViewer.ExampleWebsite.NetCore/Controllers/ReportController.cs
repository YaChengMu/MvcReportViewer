using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace AlanJuden.MvcReportViewer.ExampleWebsite.NetCore.Controllers
{
    public class ReportController : AlanJuden.MvcReportViewer.ReportController
    {
        private readonly IWebHostEnvironment webHostEnvironment;

        public ReportController(IWebHostEnvironment webHostEnvironment)
        {
            this.webHostEnvironment = webHostEnvironment;
        }

        protected override ICredentials NetworkCredentials
        {
            get
            {
                return new System.Net.NetworkCredential("Administrator", "admin_0", "WEB");
                //return System.Net.CredentialCache.DefaultNetworkCredentials;
            }
        }
        protected override string ReportServerUrl
        {
            get
            {
                return "http://192.168.101.11/ReportServer";
            }
        }
        protected override Encoding Encoding => System.Text.Encoding.UTF8;

        public ActionResult MyReport(string namedParameter1, string namedParameter2)
        {
            var model = this.GetReportViewerModel(Request);
            
            model.ReportPath = "/宿舍房间统计";
            model.AddParameter("SchoolArea", "");
            //model.AddParameter("Parameter2", namedParameter2);

            return View("ReportViewer", model);
        }
    }
}