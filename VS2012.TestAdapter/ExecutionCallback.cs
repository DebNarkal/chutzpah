using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Chutzpah.Coverage;
using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Wrappers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TestCase = Chutzpah.Models.TestCase;

namespace Chutzpah.VS2012.TestAdapter
{
    public class ExecutionCallback : RunnerCallback
    {
        private readonly IFrameworkHandle frameworkHandle;
        private readonly IRunContext runContext;

        public ExecutionCallback(IFrameworkHandle frameworkHandle, IRunContext runContext)
        {
            this.frameworkHandle = frameworkHandle;
            this.runContext = runContext;
        }

        public override void FileError(TestError error)
        {
            frameworkHandle.SendMessage(TestMessageLevel.Error, GetFileErrorMessage(error));
        }

        public override void FileLog(TestLog log)
        {
            frameworkHandle.SendMessage(TestMessageLevel.Informational, GetFileLogMessage(log));
        }

        public override void ExceptionThrown(Exception exception, string fileName)
        {
            frameworkHandle.SendMessage(TestMessageLevel.Error, GetExceptionThrownMessage(exception, fileName));
        }

        public override void TestStarted(TestCase test)
        {
            var testCase = test.ToVsTestCase();

            // The test case is starting
            frameworkHandle.RecordStart(testCase);

        }

        public override void TestFinished(TestCase test)
        {
            var testCase = test.ToVsTestCase();
            var result = test.ToVsTestResult();
            var outcome = ChutzpahExtensionMethods.ToVsTestOutcome(test.TestOutcome);


            frameworkHandle.RecordResult(result);

            // The test case is done
            frameworkHandle.RecordEnd(testCase, outcome);
        }

        public override void TestSuiteFinished(TestCaseSummary testResultsSummary)
        {
            base.TestSuiteFinished(testResultsSummary);

            if(!runContext.IsDataCollectionEnabled || testResultsSummary.CoverageObject == null)
            {
                return;
            }

            try
            {
                // If we do not have a solutiondirectory, we assume that we are running in tfs build
                // In that case we only write to the testrundirectory and do not open a browser
                if(string.IsNullOrEmpty(runContext.SolutionDirectory))
                {
                    ChutzpahTracer.TraceInformation("Chutzpah runs in TFSBuild, writing coverage file to {0}", runContext.TestRunDirectory);

                    var directory = runContext.TestRunDirectory;
                    CoverageOutputGenerator.WriteHtmlFile(Path.Combine(directory, Constants.CoverageHtmlFileName), testResultsSummary.CoverageObject);

                }
                else
                {
                    ChutzpahTracer.TraceInformation("Chutzpah runs not in TFSBuild opening coverage file in browser");

                    var directory = runContext.SolutionDirectory;
                    var coverageHtmlFile = CoverageOutputGenerator.WriteHtmlFile(Path.Combine(directory, Constants.CoverageHtmlFileName), testResultsSummary.CoverageObject);
                    var processHelper = new ProcessHelper();

                    processHelper.LaunchFileInBrowser(coverageHtmlFile);
                }
            }
            catch(Exception e)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Error, string.Format("Error while writing coverage output: {0}", e));
            }
        }

    }
}