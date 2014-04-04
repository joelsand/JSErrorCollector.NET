﻿using System.Diagnostics;
using System.Reflection;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace JSErrorCollector
{
    public class JavaScriptError
    {
        private readonly string errorMessage;
        private readonly string sourceName;
        private readonly int lineNumber;

        public JavaScriptError(Dictionary<string, object> map)
        {
            this.errorMessage = map["errorMessage"].ToString();
            this.sourceName = map["sourceName"].ToString();
            this.lineNumber = int.Parse(map["lineNumber"].ToString());
        }

        public JavaScriptError(string errorMessage, string sourceName, int lineNumber)
        {
            this.errorMessage = errorMessage;
            this.sourceName = sourceName;
            this.lineNumber = lineNumber;
        }

        public string ErrorMessage
        {
            get
            {
                return this.errorMessage;
            }
        }

        public string SourceName
        {
            get
            {
                return this.sourceName;
            }
        }

        public int LineNumber
        {
            get
            {
                return this.lineNumber;
            }
        }

        public override int GetHashCode()
        {
            string str = this.ToString();
            return str.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != this.GetType())
                return false;

            return string.Equals(this.ToString(), obj.ToString());
        }

        public override string ToString()
        {
            return errorMessage + " [" + sourceName + ":" + lineNumber + "]";
        }

        /// <summary>
        /// Gets the collected JavaScript errors that have occurred since last call to this method.
        /// </summary>
        /// <param name="driver">the driver providing the possibility to retrieved JavaScript errors (see AddExtension(FirefoxProfile)).</param>
        /// <returns>the errors or an empty list if the driver doesn't provide access to the JavaScript errors</returns>
        public static IEnumerable<JavaScriptError> ReadErrors(IWebDriver driver)
        {
            const string script = "return window.JSErrorCollector_errors ? window.JSErrorCollector_errors.pump() : []";
            ReadOnlyCollection<object> errors = (ReadOnlyCollection<object>)((IJavaScriptExecutor)driver).ExecuteScript(script);
            List<JavaScriptError> response = new List<JavaScriptError>();
            foreach (object rawError in errors)
            {
                response.Add(new JavaScriptError((Dictionary<string, object>)rawError));
            }
            return response;
        }

        private const string xpiFilename = "JSErrorCollector.xpi";
        private const string xpiResourceName = "JSErrorCollector.JSErrorCollector.xpi";
        private static readonly string extractedXpiPath;

        /// <summary>
        /// Adds the Firefox extension collecting JS errors to the profile, which allows later use of
        /// ReadErrors(WebDriver), explicitly specifying the directory in which to find the XPI.
        /// </summary>
        /// <example><code>
        /// FirefoxProfile profile = new FirefoxProfile();
        /// JavaScriptError.AddExtension(profile, "./xpiDirectory");
        /// IWebDriver driver = new FirefoxDriver(profile);
        /// </code></example>
        [Obsolete("The JSErrorCollector DLL now includes the XPI, so a directory to load from is no longer required")]
        public static void AddExtension(FirefoxProfile ffProfile, string xpiDirectory)
        {
            ffProfile.AddExtension(Path.Combine(xpiDirectory, xpiFilename));
        }

        /// <summary>
        /// Adds the Firefox extension collecting JS errors to the profile, which allows later use of
        /// ReadErrors(WebDriver).
        /// </summary>
        /// <example><code>
        /// FirefoxProfile profile = new FirefoxProfile();
        /// JavaScriptError.AddExtension(profile);
        /// IWebDriver driver = new FirefoxDriver(profile);
        /// </code></example>
        public static void AddExtension(FirefoxProfile ffProfile)
        {
            ffProfile.AddExtension(extractedXpiPath);
        }

        static JavaScriptError() {
            extractedXpiPath = ExtractXpiToTempFile();
        }

        private static string ExtractXpiToTempFile()
        {
            var xpiPath = Path.Combine(Path.GetTempPath(), xpiFilename);

            byte[] xpiData;
            using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(xpiResourceName)) {
                xpiData = BytesFromResource(resource);
            }

            // Only write out the XPI if it's not already present (or it's present but different)
            if (!File.Exists(xpiPath) || !File.ReadAllBytes(xpiPath).SequenceEqual(xpiData)) {
                WriteDataToFile(xpiData, xpiPath);
            }

            return xpiPath;
        }

        private static void WriteDataToFile(byte[] data, string path) {
            using (var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                file.Write(data, 0, data.Count());
            }
        }

        private static byte[] BytesFromResource(Stream resource) {
            byte[] data = new byte[resource.Length];
            resource.Read(data, 0, (int) resource.Length);
            return data;
        }
    }
}
