// (C) Copyright 2015 by Microsoft 
//
using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Contrib;
using System.Net;
// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(EntitlementAPI_AutoCAD.MyCommands))]

namespace EntitlementAPI_AutoCAD
{

    // This class is instantiated by AutoCAD for each document when
    // a command is called by the user the first time in the context
    // of a given document. In other words, non static data in this class
    // is implicitly per-document!
    public class MyCommands
    {

        static private bool verifyEntitlement(string appId, string userId)
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

        

        [LispFunction("TestEntitlementLisp", "TestEntitlementLisp")]
        public int TestEntitlement_Lisp(ResultBuffer args) // This method can have any name
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            //app id
            //replace your App id here...
			//contact appsubmissions@autodesk.com for the App Id
            String appID = "<appstore.exchange.autodesk.com:Your App id>";

            //Steps to get the user id
            String userID = Application.GetSystemVariable("ONLINEUSERID") as String;

            //Not logged in with Autodesk Id, hence we can not get user id
            if (userID.Equals(""))
            {
                ed.WriteMessage("Entitlement API check failed. Please log-in to Autodesk 360\n");

                //you can choose to return any number like 100 for fail & 200 for pass..
                return 0;
            }

            bool isValid = verifyEntitlement(appID, userID);


            if (isValid)
            {
                //User has downloaded the App from the store and hence is a valid user...
                ed.WriteMessage("\nEntitlement API check successful \n");
                ed.WriteMessage("User can use the App\n");
            }
            else
            {
                //Not a valid user. Entitlement check failed.   
                ed.WriteMessage("Not a valid user. Entitlement API check failed\n");
                return 0;
            }

            return 1;
        }

        //command to be called

        [CommandMethod("ADNPLUGINS", "NETAppCommand", CommandFlags.Modal)]
        static public void TestEntitlement_NET()
        {
            //if (!verifyEntitlement())
            //{
            //    return ;
            //}
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            //app id
            //replace your App id here...
            String appID = "appstore.exchange.autodesk.com:adncopyprotectiondemoapp_windows32and64";

            //Steps to get the user id
            String userID = Application.GetSystemVariable("ONLINEUSERID") as String;

            //Not logged in with Autodesk Id, hence we can not get user id
            if (userID.Equals(""))
            {
                ed.WriteMessage("Entitlement API check failed. Please log-in to Autodesk 360\n");

                //you can choose to return any number like 100 for fail & 200 for pass..
                return;
            }

            bool isValid = verifyEntitlement(appID, userID);

           
            if (isValid)
            {
                //User has downloaded the App from the store and hence is a valid user...
                ed.WriteMessage("\nEntitlement API check successful \n");
                ed.WriteMessage("User can use the App\n");
            }
            else
            {
                //Not a valid user. Entitlement check failed.   
                ed.WriteMessage("Not a valid user. Entitlement API check failed\n");
                return ;
            }
            

            //continue the function
            return ;
        }

    }

    class EntitlementResult
    {
        public String UserId { get; set; }
        public String AppId { get; set; }
        public bool IsValid { get; set; }
        public String Message { get; set; }

    }

}
