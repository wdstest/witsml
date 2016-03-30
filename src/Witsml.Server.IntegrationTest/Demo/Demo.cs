﻿using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Demo
{
    [TestClass]
    public class Demo
    {
        private DevKit141Aspect DevKit;
        private string BaseDir;
        private string DataDir;

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect();

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            BaseDir = AppDomain.CurrentDomain.BaseDirectory;
            DataDir = BaseDir + @"\Demo\Data\";
        }

        public void Add_Log_from_file(string xmlfile)
        {
            var xmlin = File.ReadAllText(xmlfile);
            var response = DevKit.AddToStore(ObjectTypes.Log, xmlin, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        public void Add_Well_from_file(string xmlfile)
        {
            var xmlin = File.ReadAllText(xmlfile);
            var response = DevKit.AddToStore(ObjectTypes.Well, xmlin, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        public void Add_Wellbore_from_file(string xmlfile)
        {
            var xmlin = File.ReadAllText(xmlfile);
            var response = DevKit.AddToStore(ObjectTypes.Wellbore, xmlin, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        /// <summary>
        /// Add <see cref="Well"/> and <see cref="Wellbore"/> object to the store.
        /// </summary>
        [TestMethod]
        public void Add_parents()
        {
            string[] wellFiles = Directory.GetFiles(DataDir, "*_Well.xml");

            foreach (string xmlfile in wellFiles)
            {
                Add_Well_from_file(xmlfile);
            }

            string[] wellboreFiles = Directory.GetFiles(DataDir, "*_Wellbore.xml");
            foreach (string xmlfile in wellboreFiles)
            {
                Add_Wellbore_from_file(xmlfile);
            }
        }

        /// <summary>
        /// Add <see cref="Logs"/> to the store
        /// </summary>
        [TestMethod]
        public void Add_Logs()
        {
            string[] logFiles = Directory.GetFiles(DataDir, "*_Log.xml");

            foreach (string xmlfile in logFiles)
            {
                Add_Log_from_file(xmlfile);
            }
        }
    }
}
