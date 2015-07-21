using System;
using System.Runtime.InteropServices;
using Inventor;
using Microsoft.Win32;
using System.Text;
using RestSharp;
using RestSharp.Deserializers;


namespace EntitlementAPIAddInCS
{
    /// <summary>
    /// This is the primary AddIn Server class that implements the ApplicationAddInServer interface
    /// that all Inventor AddIns are required to implement. The communication between Inventor and
    /// the AddIn is via the methods on this interface.
    /// </summary>
    [GuidAttribute("e8b7e56a-a693-4a27-8c9c-c4ceff942f1f")]
    public class StandardAddInServer : Inventor.ApplicationAddInServer
    {
        private ButtonDefinition m_sampleButton1;

        private UserInterfaceEvents m_uiEvents;

        // Inventor application object.
        private Inventor.Application m_inventorApplication;

        public StandardAddInServer()
        {
        }

        #region ApplicationAddInServer Members

        public void Activate(Inventor.ApplicationAddInSite addInSiteObject, bool firstTime)
        {
            // This method is called by Inventor when it loads the addin.
            // The AddInSiteObject provides access to the Inventor Application object.
            // The FirstTime flag indicates if the addin is loaded for the first time.

            // Initialize AddIn members.
            m_inventorApplication = addInSiteObject.Application;

            // TODO: Add ApplicationAddInServer.Activate implementation.
            // e.g. event initialization, command creation etc.
            // Connect to the user-interface events to handle a ribbon reset.

            {
                m_uiEvents = m_inventorApplication.UserInterfaceManager.UserInterfaceEvents;
                m_uiEvents.OnResetRibbonInterface +=m_uiEvents_OnResetRibbonInterface;

                stdole.IPictureDisp largeIcon = PictureDispConverter.ToIPictureDisp(InvAddIn.Resource1.Large);
                stdole.IPictureDisp smallIcon = PictureDispConverter.ToIPictureDisp(InvAddIn.Resource1.Small);
                Inventor.ControlDefinitions controlDefs = m_inventorApplication.CommandManager.ControlDefinitions;
                m_sampleButton1 = controlDefs.AddButtonDefinition("License check", "Entitlement API", CommandTypesEnum.kShapeEditCmdType, AddInClientID(), "Entitlement api", "Entitlement api", smallIcon, largeIcon);
                m_sampleButton1.OnExecute +=m_sampleButton1_OnExecute;

                // Add to the user interface, if it's the first time.
                if (firstTime)
                {
                    AddToUserInterface();
                }
            }

        }

        // Sub where the user-interface creation is done.  This is called when
        // the add-in loaded and also if the user interface is reset.
        private void AddToUserInterface()
        {
            try
            {
                Ribbon partRibbon = m_inventorApplication.UserInterfaceManager.Ribbons["Part"];
                RibbonTab toolsTab = partRibbon.RibbonTabs["id_TabTools"];
                RibbonPanel customPanel = toolsTab.RibbonPanels.Add("Entitlement Test", "MySample", AddInClientID());
                customPanel.CommandControls.AddButton(m_sampleButton1);
            }
            catch (Exception ex)
            {
            }
        }

        private void m_uiEvents_OnResetRibbonInterface(NameValueMap Context)
        {
            AddToUserInterface();
        }

        //public const string _baseApiUrl = @"https://apps.exchange.autodesk.com/";
        // This is the id of your app. 
       // public const string _appId = @"";


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

        private void m_sampleButton1_OnExecute(NameValueMap Context)
        {
            try
            {
                string userName;
                string userId = WebServicesUtils.GetUserId(out userName);

				//replace your App id here...
				//contact appsubmissions@autodesk.com for the App Id
                string appId = @"<your App id>";

                bool isValid = verifyEntitlement(appId, userId);
                

                // Get the auth token. 
                if (isValid)
                {
                    System.Windows.Forms.MessageBox.Show("User is entitled to use the App");
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("User do not have entitlement to use the App");
                }
                
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }
        }

        public void Deactivate()
        {
            // This method is called by Inventor when the AddIn is unloaded.
            // The AddIn will be unloaded either manually by the user or
            // when the Inventor session is terminated

            // TODO: Add ApplicationAddInServer.Deactivate implementation

            m_uiEvents.OnResetRibbonInterface -= m_uiEvents_OnResetRibbonInterface;

            // Release objects.
            m_inventorApplication = null;
            m_uiEvents = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void ExecuteCommand(int commandID)
        {
            // Note:this method is now obsolete, you should use the 
            // ControlDefinition functionality for implementing commands.
        }

        public object Automation
        {
            // This property is provided to allow the AddIn to expose an API 
            // of its own to other programs. Typically, this  would be done by
            // implementing the AddIn's API interface in a class and returning 
            // that class object through this property.

            get
            {
                // TODO: Add ApplicationAddInServer.Automation getter implementation
                return null;
            }
        }

        #endregion

        // This function uses reflection to get the GuidAttribute associated with the add-in.
        public String AddInClientID()
        {
            string guid = "";
            try
            {
                Type t = typeof(EntitlementAPIAddInCS.StandardAddInServer);
                object[] customAttributes = t.GetCustomAttributes(typeof(GuidAttribute), false);
                GuidAttribute guidAttribute = (GuidAttribute)customAttributes[0];
                guid = "{" + guidAttribute.Value.ToString() + "}";
            }
            catch
            {
            }

            return guid;
        }


    }

    #region "Image Converter"
    // Class used to convert bitmaps and icons from their .Net native types into
    // an IPictureDisp object which is what the Inventor API requires. A typical
    // usage is shown below where MyIcon is a bitmap or icon that's available
    // as a resource of the project.
    //
    // Dim smallIcon As stdole.IPictureDisp = PictureDispConverter.ToIPictureDisp(My.Resources.MyIcon)

    public sealed class PictureDispConverter
    {
        [DllImport("OleAut32.dll", EntryPoint = "OleCreatePictureIndirect", ExactSpelling = true, PreserveSig = false)]
        private static extern stdole.IPictureDisp OleCreatePictureIndirect([MarshalAs(UnmanagedType.AsAny)]
object picdesc, ref Guid iid, [MarshalAs(UnmanagedType.Bool)]
bool fOwn);


        static Guid iPictureDispGuid = typeof(stdole.IPictureDisp).GUID;
        private sealed class PICTDESC
        {
            private PICTDESC()
            {
            }

            //Picture Types
            public const short PICTYPE_BITMAP = 1;

            public const short PICTYPE_ICON = 3;
            [StructLayout(LayoutKind.Sequential)]
            public class Icon
            {
                internal int cbSizeOfStruct = Marshal.SizeOf(typeof(PICTDESC.Icon));
                internal int picType = PICTDESC.PICTYPE_ICON;
                internal IntPtr hicon = IntPtr.Zero;
                internal int unused1;

                internal int unused2;
                internal Icon(System.Drawing.Icon icon)
                {
                    this.hicon = icon.ToBitmap().GetHicon();
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            public class Bitmap
            {
                internal int cbSizeOfStruct = Marshal.SizeOf(typeof(PICTDESC.Bitmap));
                internal int picType = PICTDESC.PICTYPE_BITMAP;
                internal IntPtr hbitmap = IntPtr.Zero;
                internal IntPtr hpal = IntPtr.Zero;

                internal int unused;
                internal Bitmap(System.Drawing.Bitmap bitmap)
                {
                    this.hbitmap = bitmap.GetHbitmap();
                }
            }
        }

        public static stdole.IPictureDisp ToIPictureDisp(System.Drawing.Icon icon)
        {
            PICTDESC.Icon pictIcon = new PICTDESC.Icon(icon);
            return OleCreatePictureIndirect(pictIcon, ref iPictureDispGuid, true);
        }

        public static stdole.IPictureDisp ToIPictureDisp(System.Drawing.Bitmap bmp)
        {
            PICTDESC.Bitmap pictBmp = new PICTDESC.Bitmap(bmp);
            return OleCreatePictureIndirect(pictBmp, ref iPictureDispGuid, true);
        }
    }
    #endregion

    class WebServicesUtils
    {
        [DllImport("AdWebServices", EntryPoint = "GetUserId", CharSet = CharSet.Unicode)]
        private static extern int AdGetUserId(StringBuilder userid, int buffersize);

        [DllImport("AdWebServices", EntryPoint = "IsWebServicesInitialized")]
        private static extern bool AdIsWebServicesInitialized();

        [DllImport("AdWebServices", EntryPoint = "InitializeWebServices")]
        private static extern void AdInitializeWebServices();

        [DllImport("AdWebServices", EntryPoint = "IsLoggedIn")]
        private static extern bool AdIsLoggedIn();

        [DllImport("AdWebServices", EntryPoint = "GetLoginUserName", CharSet = CharSet.Unicode)]
        private static extern int AdGetLoginUserName(StringBuilder username, int buffersize);

        internal static string _GetUserId()
        {
            int buffersize = 128; //should be long enough for userid
            StringBuilder sb = new StringBuilder(buffersize);
            int len = AdGetUserId(sb, buffersize);
            sb.Length = len;

            return sb.ToString();
        }

        internal static string _GetUserName()
        {
            int buffersize = 128; //should be long enough for username 
            StringBuilder sb = new StringBuilder(buffersize);
            int len = AdGetLoginUserName(sb, buffersize);
            sb.Length = len;

            return sb.ToString();
        }

        public static string GetUserId(out string userName)
        {
            AdInitializeWebServices();

            if (!AdIsWebServicesInitialized())
                throw new Exception("Could not initialize the web services component.");

            if (!AdIsLoggedIn())
                throw new Exception("User is not logged in. Please log-in to Autodesk 360");

            string userId = _GetUserId();
            if (userId == "")
            {
                throw new Exception("Could not get user id. Please log-in to Autodesk 360");
            }

            userName = _GetUserName();
            if (userName == "")
            {
                throw new Exception("Could not get user name. Please log-in to Autodesk 360");
            }

            return userId;
        }
    }

    [Serializable]
    public class EntitlementResult
    {
        public string UserId { get; set; }
        public string AppId { get; set; }
        public bool IsValid { get; set; }
        public string Message { get; set; }
    }
}
