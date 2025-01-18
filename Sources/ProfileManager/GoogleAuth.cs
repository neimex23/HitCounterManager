﻿//MIT License

//Copyright (c) 2022-2024 Ezequiel Medina

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using Google.Apis.Auth.OAuth2;
using Google.Apis.Oauth2.v2;
using Google.Apis.Oauth2.v2.Data;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Runtime.InteropServices.ComTypes;
using System.Diagnostics;
using System.Web;
using Google.Apis.Download;
using System.Drawing;

namespace AutoSplitterCore
{
    public partial class GoogleAuth : ReaLTaiizor.Forms.LostForm
    {
        static string[] Scopes = { "https://www.googleapis.com/auth/userinfo.email", DriveService.Scope.Drive, DriveService.Scope.DriveReadonly };
        static string ApplicationName = "autosplittercore";

        private readonly string machineName = Environment.MachineName;
        DriveService driveService = null;
        Oauth2Service oauth2Service = null;
        SheetsService Sheetsservice;
        UserCredential credential;

        SaveModule saveModule;

        string EmailLoged = null; 

        public GoogleAuth(SaveModule saveModule)
        {
            InitializeComponent();
            this.saveModule = saveModule;
        }

        private void GoogleAuth_Load(object sender, EventArgs e)
        {
            checkedListBoxGames.Items.Clear();
            foreach (var games in GameConstruction.GameList)
            {
                if (!games.Contains("None"))
                {
                    checkedListBoxGames.Items.Add(games);
                    checkedListBoxGamesSearch.Items.Add(games);
                }
            }
        }

        private DataAutoSplitter DeserializeXmlFile(string filePath)
        {
            using (Stream myStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                XmlSerializer formatter = new XmlSerializer(typeof(DataAutoSplitter),
                    new Type[] { typeof(DTSekiro), typeof(DTHollow), typeof(DTElden), typeof(DTDs3), typeof(DTDs2),
                         typeof(DTDs1), typeof(DTCeleste), typeof(DTCuphead), typeof(DTDishonored) });
                try
                {
                    return (DataAutoSplitter)formatter.Deserialize(myStream);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error deserializing XML file: {ex.Message}");
                }
            }
        }

        #region AuthProcess
        private void btnLogin_Click(object sender, EventArgs e)
        {
            MessageBox.Show("You must select a Google account and enable all the required permissions for the drive folder where the files will be downloaded.\nThe program will generate an encrypted key in the Google.Apis.auth folder. \nDo not disclose or transfer this key under any circumstances.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            Cursor.Current = Cursors.WaitCursor;
            Task.Run(() => Auth());
            Cursor = Cursors.Default;
        }

        private void btnForgetLogin_Click(object sender, EventArgs e)
        {
            try
            {
                var dataStore = new EncryptedDataStore("Google.Apis.Auth");

                dataStore.Clear(); // Remove all credentials

                Console.WriteLine("Logout completed. All stored credentials have been cleared.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during logout process: " + ex.Message);
                MessageBox.Show("Error during logout process:" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task Auth()
        {
            try
            {
                string googleCredentialsJson = await GetGoogleCredentials();
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(googleCredentialsJson)))
                {
                    // Authenticate with the obtained credentials
                    var dataStore = new EncryptedDataStore("Google.Apis.Auth");

                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets,
                        Scopes,
                        machineName,
                        CancellationToken.None,
                        new FileDataStoreAdapter(dataStore)
                    );


                    Console.WriteLine("Authentication completed for ´user´ machine: " + machineName);
                    CreateService();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error on Authentification process: " + e.Message);
            }
        }


        //Dev should create a appsettings.json on root source code and using "Incrusting resource method" with url of AWS_ApiGateWay with name GetGoogleCredentials_ApiGateWay = api.url
        // For more info read my investigation: https://www.notion.so/Manejo-de-Secretos-c781ca2f65c449f4b9a6aa82fef3ab0a?pvs=4 (web on Spanish language)
        public static string GetAPIUrl()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream("AutoSplitterCore.appsettings.json"))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string jsonContent = reader.ReadToEnd();
                    var jsonObject = JObject.Parse(jsonContent);
                    return jsonObject["ApiSettings"]["GetGoogleCredentials_ApiGateWay"].ToString();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error on Incrusted Resource: ", ex);
            }
        }

        public async Task<string> GetGoogleCredentials()
        {
            // API Gateway URL
            string url = GetAPIUrl();

            try
            {
                // Create content for the POST request
                var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

                // Perform the POST request to the API Gateway
                HttpResponseMessage response = await client.PostAsync(url, content);

                // Validate that the response is successful
                response.EnsureSuccessStatusCode();

                // Get the response body as a string
                string responseBody = await response.Content.ReadAsStringAsync();

                // Validate if the response contains the expected format
                const string prefix = "{\"AutoSplitterCoreAuth\":";
                if (responseBody.StartsWith(prefix))
                {
                    // Extract the value after the prefix
                    string jsonFragment = responseBody.Substring(prefix.Length).Trim();

                    // Remove the last character '}' if present
                    if (jsonFragment.EndsWith("}"))
                    {
                        jsonFragment = jsonFragment.Substring(0, jsonFragment.Length - 1).Trim();
                    }

                    // Replace escaped quotes
                    jsonFragment = jsonFragment.Replace("\\\"", "\"");

                    // Remove surrounding quotes and whitespace
                    return jsonFragment.Trim('\"').Trim();
                }

                // If it doesn't have the expected format, return the full response
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("Error making the request: " + e.Message);
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected error: " + ex.Message);
                throw;
            }
        }

        private static readonly HttpClient client = new HttpClient();

        private void CreateService()
        {
            // Create the Google Drive service
            driveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            oauth2Service = new Oauth2Service(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });


            Sheetsservice = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            Userinfo userInfo = oauth2Service.Userinfo.Get().Execute();
            EmailLoged = userInfo.Email;
            linkLabel1.Invoke(new Action(() =>
            {
                linkLabel1.Text = EmailLoged;
                AfterLoginEvents();
            }));

            groupBoxManagment.Invoke(new Action(() =>
            {
                groupBoxManagment.Enabled = true;
            }));

        }

        public async Task<bool> IsEmailBannedAsync(string email)
        {
            var spreadsheetId = "1syCG3vchr4rHu-_9vQAS6UCJaoonZ06imyPzWNCRtRU";
            var range = $"Sheet1!A2:B";
            var request = Sheetsservice.Spreadsheets.Values.Get(spreadsheetId, range);
            var response = await request.ExecuteAsync();
            var values = response.Values;

            if (values != null)
            {
                foreach (var row in values)
                {
                    if (row.Count > 0 && row[0].ToString().Equals(email, StringComparison.OrdinalIgnoreCase))
                    {
                        return true; // Email found
                    }
                }
            }

            return false; // Email not found
        }
        #endregion
        #region LoadFilesOnDrive
        private void AfterLoginEvents()
        {
            btnLogin.Enabled = false;
            btnForgetLogin.Enabled = true;
            LoadFilesFromPublicFolder();

            textBoxCurrrentProfile.Text = saveModule.GetProfileName();
            textBoxAuthor.Text = saveModule.GetAuthor();
            TextboxDescription.Text = saveModule.GetDescription();
            textBoxDate.Text = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        }

        List<ListViewItem> allFiles = new List<ListViewItem>();
        private void LoadFilesFromPublicFolder()
        {
            string folderId = "16Y9MeL_Zbi5NgfTbBvbG-7JzGpPkCEqV";
            var request = driveService.Files.List();
            request.Q = $"'{folderId}' in parents and mimeType != 'application/vnd.google-apps.folder' and trashed = false";
            request.Fields = "files(id, name, description, properties, createdTime, mimeType)";

            try
            {
                var result = request.Execute();

                listViewFiles.View = View.Details; 
                listViewFiles.FullRowSelect = true; 
                listViewFiles.GridLines = true; 

                if (listViewFiles.Columns.Count == 0)
                {
                    listViewFiles.Columns.Add("Name", 150);
                    listViewFiles.Columns.Add("Author", 100);       
                    listViewFiles.Columns.Add("Description", 200);
                    listViewFiles.Columns.Add("Games", 100);
                    listViewFiles.Columns.Add("Date", 100);
                    listViewFiles.Columns.Add("ID", 100);                  
                }

                listViewFiles.Items.Clear(); // Clean Elements

                // Process and add files to ListView
                foreach (var file in result.Files)
                {
                    var item = new ListViewItem(file.Name); // FileName

                    item.SubItems.Add(file.Properties != null && file.Properties.ContainsKey("author")
                       ? file.Properties["author"]
                       : "Unknown"); // Author
                    item.SubItems.Add(file.Description ?? "No description"); // Description

                    string games = file.Properties != null && file.Properties.ContainsKey("games")
                   ? file.Properties["games"]
                   : "None";
                    item.SubItems.Add(games); // Games                  
                    item.SubItems.Add(file.CreatedTimeDateTimeOffset?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown"); // Date of Creation
                    item.SubItems.Add(file.Id); // DriveID

                    listViewFiles.Items.Add(item);
                }

                listViewFiles.Refresh();
                allFiles = new List<ListViewItem>(listViewFiles.Items.Cast<ListViewItem>());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion
        #region UploadProfile

        private void btnUploadProfile_Click(object sender, EventArgs e)
        {
            // Profile validation
            if (saveModule.GetProfileName() == "Default" ||
                saveModule.GetAuthor() == "Owner" ||
                saveModule.GetDescription() == "Default Profile")
            {
                MessageBox.Show("You must use a profile name, author, and description different from default.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (checkedListBoxGames.CheckedItems.Count == 0) { MessageBox.Show("You must select games for the profile.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);  return; }


            // File name validation
            if (IsFileNameRepeated(saveModule.GetProfileName()))
            {
                MessageBox.Show("Profile name already exists, please use another.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var isEmailBanned = Task.Run(() => IsEmailBannedAsync(EmailLoged)).GetAwaiter().GetResult();

            if (isEmailBanned)
            {
                MessageBox.Show(
                    "You are banned from uploading profiles.\nContact the administrator to appeal.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Stop
                );
                return;
            }

            if (MessageBox.Show("Splitter Flags and run Will be Reset to upload profile", "Warning", MessageBoxButtons.OKCancel,MessageBoxIcon.Question) == DialogResult.Cancel) return;
            saveModule.ResetFlags();
            saveModule.UpdateAutoSplitterData();

            var tempData = saveModule.dataAS;
            tempData.GeneralSettings = new GeneralAutoSplitter();

            string path = Path.GetTempPath() + saveModule.GetProfileName() + ".xml";
            XmlSerializer serializer = new XmlSerializer(typeof(DataAutoSplitter));
            using (Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                serializer.Serialize(stream, tempData);
                stream.Close();
            }

            try
            {
                var parentFolderId = "16Y9MeL_Zbi5NgfTbBvbG-7JzGpPkCEqV";

                // Validate folder
                var folderRequest = driveService.Files.Get(parentFolderId);
                var folder = folderRequest.Execute();
                if (folder == null || folder.Trashed == true)
                {
                    MessageBox.Show("The specified parent folder is invalid or trashed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // File metadata
                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = Path.GetFileName(path),
                    Description = saveModule.GetDescription(),
                    Parents = new List<string> { parentFolderId },
                    Properties = new Dictionary<string, string>
                    {
                        { "author", saveModule.GetAuthor() },
                        { "date", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") },
                        { "games", string.Join(", ", checkedListBoxGames.CheckedItems.Cast<object>().Select(g => g.ToString())) }
                    }
                };

                // Determine MIME type
                string mimeType = MimeMapping.GetMimeMapping(path);

                // Upload file
                using (var stream = new FileStream(path, FileMode.Open))
                {
                    var request = driveService.Files.Create(fileMetadata, stream, mimeType);
                    request.Fields = "id";
                    var uploadResult = request.Upload();

                    if (uploadResult.Status == Google.Apis.Upload.UploadStatus.Completed)
                    {
                        MessageBox.Show("File uploaded successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadFilesFromPublicFolder();
                    }
                    else
                    {
                        MessageBox.Show($"File upload failed: {uploadResult.Status}. Error: {uploadResult.Exception?.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally {
                File.Delete(path);
            }           
        }

        public bool IsFileNameRepeated(string fileName)
        {
            try
            {
                var parentFolderId = "16Y9MeL_Zbi5NgfTbBvbG-7JzGpPkCEqV";
                var request = driveService.Files.List();
                request.Q = $"name = '{fileName}' and '{parentFolderId}' in parents and trashed = false";
                request.Fields = "files(id, name)";

                var result = request.Execute();
                return result.Files.Count > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error: {ex.Message}");
            }
        }

        #endregion
        #region DownloadFile
        public void DownloadFile(string fileId, string destinationPath)
        {
            try
            {
                var request = driveService.Files.Get(fileId);
                using (var stream = new MemoryStream())
                {
                    request.MediaDownloader.ProgressChanged += progress =>
                    {
                        switch (progress.Status)
                        {
                            case DownloadStatus.Downloading:
                                Console.WriteLine($"Downloading: {progress.BytesDownloaded} bytes...");
                                break;
                            case DownloadStatus.Completed:
                                Console.WriteLine("Download completed.");
                                break;
                            case DownloadStatus.Failed:
                                Console.WriteLine("Download failed.");
                                break;
                        }
                    };

                    request.Download(stream);

                    if (stream.Length > 0)
                    {
                        stream.Position = 0; 
                        using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                        {
                            stream.CopyTo(fileStream);
                            fileStream.Close();
                        }
                        Console.WriteLine($"File saved to {destinationPath}");
                    }
                    else
                    {
                        Console.WriteLine("Stream is empty. No data was downloaded.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file: {ex.Message}");
            }
        }

        private void listViewFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count > 0)
            {
                var selectedItem = listViewFiles.SelectedItems[0];
                var fileId = selectedItem.SubItems[5].Text; 
                var fileName = selectedItem.Text; 

                if (!fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("The Selected Filed not is a XML Format.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                try
                {
                    string tempFilePath = Path.Combine(Path.GetFullPath("./AutoSplitterProfiles"), "tmp_" + fileName); ;
                    DownloadFile(fileId, tempFilePath);

                    var configuration = DeserializeXmlFile(tempFilePath);

                    SaveModule saveModuleLocal = new SaveModule();
                    saveModuleLocal.dataAS = configuration;
                    saveModuleLocal.MainModule = this.saveModule.MainModule;

                    TextBoxSummary.Text = ProfileManager.BuildSummary(saveModuleLocal);
                    
                    File.Delete(tempFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error to process file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #region FilterListViewFileter
        List<ListViewItem> filteredItems = null;
        private void textBoxSearch_TextChanged(object sender, EventArgs e) => AplyFilters();

        private void checkedListBoxGamesSearch_ItemCheck(object sender, ItemCheckEventArgs e) => this.BeginInvoke((MethodInvoker)AplyFilters);

        private void AplyFilters() 
        {
            filteredItems = allFiles;

            // TextFilter
            string filterText = textBoxSearch.Text;
            if (!string.IsNullOrWhiteSpace(filterText))
            {
                filteredItems = filteredItems
                    .Where(item => item.SubItems[0].Text.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            // CheckBoxFilter
            var checkedItems = checkedListBoxGamesSearch.CheckedItems.Cast<string>().ToList();
            if (checkedItems.Any())
            {
                filteredItems = filteredItems
                    .Where(item => checkedItems.Any(game => item.SubItems[3].Text.Contains(game)))
                    .ToList();
            }

            //Refresh Listvirew
            listViewFiles.Items.Clear();
            listViewFiles.Items.AddRange(filteredItems.ToArray());
        }
        #endregion

        private void btnInstall_Click(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count > 0)
            {
                var selectedItem = listViewFiles.SelectedItems[0];
                var fileId = selectedItem.SubItems[5].Text;
                var fileName = selectedItem.Text;

                string tempFilePath = Path.Combine(Path.GetFullPath("./AutoSplitterProfiles"), "tmp_" + fileName); ;
                DownloadFile(fileId, tempFilePath);

                //Set current GeneralSettings before download the profile
                var configuration = DeserializeXmlFile(tempFilePath);
                configuration.GeneralSettings = saveModule.dataAS.GeneralSettings;

                string finalFilePath = Path.Combine(Path.GetFullPath("./AutoSplitterProfiles"), fileName); 

                XmlSerializer serializer = new XmlSerializer(typeof(DataAutoSplitter));
                using (Stream stream = new FileStream(finalFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    serializer.Serialize(stream, configuration);
                    stream.Close();
                }

                File.Delete(tempFilePath);
                if (MessageBox.Show("Install Complete, you can Switch the profile on Profile Manager\nDo you want close this windows?", "Susesfully",MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes) Close();
            }else
            {
                MessageBox.Show("You must Select File to download", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion
    }
}
