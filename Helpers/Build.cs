using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Xml;
using DSTBuilder.Helpers;
using System.Xml.Linq;

namespace DSTBuilder.Helpers
{
    public class Build
    {
        private string _queue = null;
        private string _file = null;

        public void StartBuild(string file)
        {
            if (File.Exists(file))
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(file);               

                //_currentQueueFile = file;

                // select the queue item
                XmlNode xNode = xDoc.SelectSingleNode("Configuration/Queue/Product");

                if (xNode != null)
                {
                    string product = xNode.Attributes["Name"].Value;
                    string version = xNode.Attributes["Version"].Value;
                    string majorMinor = xNode.Attributes["MajorMinor"].Value;
                    string engineVersion = xNode.Attributes["EngineVersion"].Value;
                    string buildType = xNode.Attributes["BuildType"].Value;
                    string buildOption = xNode.Attributes["BuildOptions"].Value;
                    string architecture = xNode.Attributes["Architecture"].Value;
                    string configuration = xNode.Attributes["Configuration"].Value;
                    string notes = xNode.Attributes["Notes"].Value;
                    string status = xNode.Attributes["Status"].Value;
                    string isCancel = xNode.Attributes["IsCancel"].Value;
                    bool skipGetSourceValue = Convert.ToBoolean(xNode.Attributes["SkipGetSource"].Value);
                    bool backupMergeModules = Convert.ToBoolean(xNode.Attributes["BackupMergeModules"].Value);

                    //TODO
                    //bool skipGetSource = false;

                    string parseStatus = status.Replace("Waiting ", "").Replace(" minute.", "").Replace(" minutes.", "");

                    if (!string.IsNullOrEmpty(parseStatus) && parseStatus != "Waiting")
                    {
                        // convert the string to an int for a delay to start the build
                        int timeToWait;
                        System.Int32.TryParse(parseStatus, out timeToWait);

                        #region GetFullProductName
                        string productFull = product;


                        #endregion GetFullProductName

                        #region Minute Text
                        string minuteText = "";

                        if (timeToWait > 1)
                        {
                            minuteText = "minutes.";
                        }
                        else
                        {
                            minuteText = "minute.";
                        }
                        #endregion Minute Text

                        // send the e-mail out that your starting the build in x amount of time, where x is the time in minutes from the UI
                        //SendEmail(GetEmail(productFull, false).ToString(), "Starting " + product + " - " + version + " build in " + timeToWait + " " + minuteText, "Starting " + product + " build in " + timeToWait.ToString() + " " + minuteText);
                        Thread.Sleep(timeToWait * 60000);
                    }

                    // change the status to In Progress and make it so that the queue item cannot be deleted by the UI
                    xNode.Attributes["Status"].Value = "In Progress";
                    xNode.Attributes["IsCancel"].Value = bool.FalseString;
                    xDoc.Save(file);
                    // DeleteLog();
                    File.Delete(file);
                    //if (File.Exists(_messages))
                    //{
                    //    File.Delete(_messages);
                    //}
                }
            }
            else
            {
                //DeleteLog();
                //AddLog("Failed to find BuildQueue!!");
                //SendEmail("eit@promodel.com", "Install Builder problem!", LogReader().ToString());
            }
        }

        public bool CheckForWork()
        {
            // check if anything is in the queue
            if (Directory.GetFiles(_queue, "*.txt").Count() > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}