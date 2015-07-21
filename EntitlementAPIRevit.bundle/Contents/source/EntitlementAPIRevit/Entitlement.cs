#region Copyright 
//
// (C) Copyright 2003-2014 by Autodesk, Inc.
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE. AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
//
// Use, duplication, or disclosure by the U.S. Government is subject to
// restrictions set forth in FAR 52.227-19 (Commercial Computer
// Software - Restricted Rights) and DFAR 252.227-7013(c)(1)(ii)
// (Rights in Technical Data and Computer Software), as applicable.
//
// Written by M.Harada 
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net; // for HttpStatusCode 
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
// adde for REST API 
using RestSharp;
using RestSharp.Deserializers;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace EntitlementAPIRevit
{
    [Regeneration(RegenerationOption.Manual)]
    class ExtApplication : IExternalApplication
    {
        BitmapImage getBitmap(string fileName)
        {
            BitmapImage bmp = new BitmapImage();
            // BitmapImage.UriSource must be in a BeginInit/EndInit block.              
            bmp.BeginInit();
            bmp.UriSource = new Uri(string.Format("pack://application:,,,/{0};component/{1}",
              Assembly.GetExecutingAssembly().GetName().Name,
              fileName));
            bmp.EndInit();

            return bmp;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            RibbonPanel panel =
                application.CreateRibbonPanel("EntitlementAPI");

            ContextualHelp contextHelp = new ContextualHelp(
                ContextualHelpType.Url, "http://www.autodesk.com/developapps");

            PushButtonData button = new PushButtonData(
                "EntitlementAPI",
                "EntitlementAPI",
                typeof(ExtApplication).Assembly.Location,
                "EntitlementAPIRevit.Commands");
            button.Image = button.LargeImage =
              getBitmap("appicon2.png");
            button.ToolTip = "This function lets you to check the Entitlement of the user";
            button.LongDescription = "Using this functionality you can create simple copy protection mechanism ";
            button.SetContextualHelp(contextHelp);

            panel.AddItem(button);

            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Commands: IExternalCommand 
    {
        // Set values specific to the environment 
        public const string _baseApiUrl = @"https://apps.exchange.autodesk.com/";
        // This is the id of your app. 
		//replace your App id here...
		//contact appsubmissions@autodesk.com for the App Id
        public const string _appId = @"<your App id>";

        // Command to check an entitlement 
        public Autodesk.Revit.UI.Result Execute(
            ExternalCommandData commandData,
            ref string message, 
            Autodesk.Revit.DB.ElementSet elements)
        {
            // Get hold of the top elements 
            UIApplication uiApp = commandData.Application;
            Application rvtApp = uiApp.Application;

            // Check to see if the user is logged in.
            if(!Application.IsLoggedIn) {
                TaskDialog.Show("Entitlement API", "Please login to Autodesk 360 first\n");
                return Result.Failed; 
            }

            // Get the user id, and check entitlement 
            string userId = rvtApp.LoginUserId;
            bool isValid = verifyEntitlement(_appId, userId);

            if (isValid)
            {
                // The usert has a valid entitlement, i.e., 
                // if paid app, purchase the app from the store.
                TaskDialog.Show("Entitlement API", "User is entitled to use the App"); 
            }
            else
            {
                TaskDialog.Show("Entitlement API", "User do not have entitlement to use the App"); 
            }

            //// For now, display the result
            //string msg = "userId = " + userId
            //    + "\nappId = " + _appId 
            //    + "\nisValid = " + isValid.ToString(); 
            //TaskDialog.Show("Entitlement API", msg); 

            return Result.Succeeded; 
        }

        ///========================================================
        /// URL: https://apps.exchange.autodesk.com/webservices/checkentitlement
        /// 
        /// Method: GET
        /// 
        /// Sample response 
        /// {
        /// "UserId":"2N5FMZW9CCED",
        /// "AppId":"appstore.exchange.autodesk.com:autodesk360:en",
        /// "IsValid":false,
        /// "Message":"Ok"
        /// }
        /// ========================================================

        private bool verifyEntitlement(string appId, string userId)
        {
            // REST API call for the entitlement API.
            // We are using RestSharp for simplicity.
            // You may choose to use other library. 
             
            // (1) Build request 
            var client = new RestClient();
            client.BaseUrl = new System.Uri("https://apps.exchange.autodesk.com");

            // Set resource/end point
            var request = new RestRequest();
            request.Resource = "webservices/checkentitlement";
            request.Method = Method.GET;

            // Add parameters 
            request.AddParameter("userid", userId);
            request.AddParameter("appid", appId);

            // (2) Execute request and get response
            IRestResponse<EntitlementResult> response = client.Execute<EntitlementResult>(request);

            // Get the auth token. 
            bool isValid = false;
            if (response.Data != null && response.Data.IsValid)
            {
                isValid = true;
            }

            //  
            return isValid; 
        }

        // To purse response  
        [Serializable]
        public class EntitlementResult 
        {
            public string UserId { get; set; }
            public string AppId { get; set; }   
            public bool IsValid { get; set; }
            public string Message { get; set; } 
        }

    }
    public class CommandAvailability : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(
          UIApplication applicationData,
          CategorySet selectedCategories
        )
        {
            UIDocument uiDoc = applicationData.ActiveUIDocument;

            if (uiDoc == null || uiDoc.Document.IsFamilyDocument)
                return false;

            switch (uiDoc.Document.ActiveView.ViewType)
            {
                case ViewType.AreaPlan:
                case ViewType.CeilingPlan:
                case ViewType.EngineeringPlan:
                case ViewType.FloorPlan:
                    return true;
            }

            return false;
        }
    }
}
