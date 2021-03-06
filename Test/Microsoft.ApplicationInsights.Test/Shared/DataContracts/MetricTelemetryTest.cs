﻿namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
#if !NETCOREAPP1_1
    using CompareLogic = KellermanSoftware.CompareNetObjects.CompareLogic;
#endif
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.TestFramework;

    [TestClass]
    public class MetricTelemetryTest
    {
        [TestMethod]
        public void MetricTelemetryIsPublic()
        {
            Assert.IsTrue(typeof(MetricTelemetry).GetTypeInfo().IsPublic);
        }

        [TestMethod]
        public void MetricTelemetryImplementsITelemetryContract()
        {
            var test = new ITelemetryTest<MetricTelemetry, AI.MetricData>();
            test.Run();
        }

        [TestMethod]
        public void EventTelemetryReturnsNonNullContext()
        {
            MetricTelemetry item = new MetricTelemetry();
            Assert.IsNotNull(item.Context);
        }

#pragma warning disable CS0618
        [TestMethod]
        public void MetricTelemetrySuppliesConstructorThatTakesNameAndValueToSimplifyAdvancedScenarios()
        {
            var instance = new MetricTelemetry("Test Metric", 4.2);

            Assert.AreEqual("Test Metric", instance.Name);
            Assert.AreEqual(4.2, instance.Value);
        }
#pragma warning restore CS0618

        [TestMethod]
        public void MetricTelemetrySuppliesPropertiesForCustomerToSendAggregatedMetric()
        {
#pragma warning disable CS0618
            var instance = new MetricTelemetry("Test Metric", 4.2);
#pragma warning restore CS0618

            instance.Count = 5;
            instance.Min = 1.2;
            instance.Max = 6.4;
            instance.StandardDeviation = 0.5;
            Assert.AreEqual(5, instance.Count);
            Assert.AreEqual(1.2, instance.Min);
            Assert.AreEqual(6.4, instance.Max);
            Assert.AreEqual(0.5, instance.StandardDeviation);
        }

        [TestMethod]
        public void AggregateMetricTelemetrySerializesToJsonCorrectly()
        {
            var expected = new MetricTelemetry();

            expected.Name = "My Page";
#pragma warning disable CS0618
            expected.Value = 42;
#pragma warning restore CS0618
            expected.Count = 5;
            expected.Min = 1.2;
            expected.Max = 6.4;
            expected.StandardDeviation = 0.5;
            expected.Properties.Add("Property1", "Value1");

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<MetricTelemetry, AI.MetricData>(expected);

            Assert.AreEqual(typeof(AI.MetricData).Name, item.data.baseType);

            Assert.AreEqual(2, item.data.baseData.ver);
            Assert.AreEqual(1, item.data.baseData.metrics.Count);
            Assert.AreEqual(expected.Name, item.data.baseData.metrics[0].name);
            Assert.AreEqual(AI.DataPointType.Aggregation, item.data.baseData.metrics[0].kind);
#pragma warning disable CS0618
            Assert.AreEqual(expected.Value, item.data.baseData.metrics[0].value);
#pragma warning restore CS0618
            Assert.AreEqual(expected.Count.Value, item.data.baseData.metrics[0].count.Value);
            Assert.AreEqual(expected.Min.Value, item.data.baseData.metrics[0].min.Value);
            Assert.AreEqual(expected.Max.Value, item.data.baseData.metrics[0].max.Value);
            Assert.AreEqual(expected.StandardDeviation.Value, item.data.baseData.metrics[0].stdDev.Value);

            AssertEx.AreEqual(expected.Properties.ToArray(), item.data.baseData.properties.ToArray());
        }

        [TestMethod]
        public void MetricTelemetrySuppliesConstructorThatAllowsToFullyPopulateAggregationData()
        {
            var instance = new MetricTelemetry(
                name: "Test Metric", 
                count: 4, 
                sum: 40, 
                min: 5, 
                max: 15, 
                standardDeviation: 4.2);

            Assert.AreEqual("Test Metric", instance.Name);
            Assert.AreEqual(4, instance.Count);
            Assert.AreEqual(40, instance.Sum);
            Assert.AreEqual(5, instance.Min);
            Assert.AreEqual(15, instance.Max);
            Assert.AreEqual(4.2, instance.StandardDeviation);
        }

        [TestMethod]
        public void MetricTelemetrySuppliesPropertiesForCustomerToSendAggregionData()
        {
            var instance = new MetricTelemetry();

            instance.Name = "Test Metric";
            instance.Count = 4;
            instance.Sum = 40;
            instance.Min = 5.0;
            instance.Max = 15.0;
            instance.StandardDeviation = 4.2;

            Assert.AreEqual("Test Metric", instance.Name);
            Assert.AreEqual(4, instance.Count);
            Assert.AreEqual(40, instance.Sum);
            Assert.AreEqual(5, instance.Min);
            Assert.AreEqual(15, instance.Max);
            Assert.AreEqual(4.2, instance.StandardDeviation);
        }

        [TestMethod]
        public void MetricTelemetrySerializesStructuredIKeyCorrectlyPreservingCaseOfPrefix()
        {
            var metricTelemetry = new MetricTelemetry();
            metricTelemetry.Context.InstrumentationKey = "AIC-" + Guid.NewGuid().ToString();
            ((ITelemetry)metricTelemetry).Sanitize();

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<MetricTelemetry, AI.MetricData>(metricTelemetry);

            Assert.AreEqual(metricTelemetry.Context.InstrumentationKey, item.iKey);
        }

        [TestMethod]
        public void SanitizeWillTrimAppropriateFields()
        {
            MetricTelemetry telemetry = new MetricTelemetry();
            telemetry.Name = new string('Z', Property.MaxNameLength + 1);
            telemetry.Properties.Add(new string('X', Property.MaxDictionaryNameLength) + 'X', new string('X', Property.MaxValueLength + 1));
            telemetry.Properties.Add(new string('X', Property.MaxDictionaryNameLength) + 'Y', new string('X', Property.MaxValueLength + 1));

            ((ITelemetry)telemetry).Sanitize();

            Assert.AreEqual(new string('Z', Property.MaxNameLength), telemetry.Name);

            Assert.AreEqual(2, telemetry.Properties.Count);
            var t = new SortedList<string, string>(telemetry.Properties);

            Assert.AreEqual(new string('X', Property.MaxDictionaryNameLength), t.Keys.ToArray()[1]);
            Assert.AreEqual(new string('X', Property.MaxValueLength), t.Values.ToArray()[1]);
            Assert.AreEqual(new string('X', Property.MaxDictionaryNameLength - 3) + "1", t.Keys.ToArray()[0]);
            Assert.AreEqual(new string('X', Property.MaxValueLength), t.Values.ToArray()[0]);
        }

        [TestMethod]
        public void SanitizePopulatesNameWithErrorBecauseItIsRequiredByEndpoint()
        {
            var telemetry = new MetricTelemetry { Name = null };

            ((ITelemetry)telemetry).Sanitize();

            Assert.AreEqual("n/a", telemetry.Name);
        }

        [TestMethod]
        public void SerializeWritesNullValuesAsExpectedByEndpoint()
        {
            MetricTelemetry original = new MetricTelemetry();
            original.Name = null;
            original.Max = null;
            original.Min = null;
            original.Count = null;
            original.StandardDeviation = null;
            ((ITelemetry)original).Sanitize();
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<MetricTelemetry, AI.MetricData>(original);

            Assert.AreEqual(2, item.data.baseData.ver);
        }

#pragma warning disable CS0618
        [TestMethod]
        public void SanitizeReplacesNaNValueOn0()
        {
            MetricTelemetry original = new MetricTelemetry("test", double.NaN);
            ((ITelemetry)original).Sanitize();

            Assert.AreEqual(0, original.Value);
        }
#pragma warning restore CS0618

        [TestMethod]
        public void SanitizeReplacesNaNMinOn0()
        {
            MetricTelemetry original = new MetricTelemetry { Min = double.NaN };
            ((ITelemetry)original).Sanitize();

            Assert.AreEqual(0, original.Min.Value);
        }

        [TestMethod]
        public void SanitizeReplacesNaNMaxOn0()
        {
            MetricTelemetry original = new MetricTelemetry { Max = double.NaN };
            ((ITelemetry)original).Sanitize();

            Assert.AreEqual(0, original.Max.Value);
        }

        [TestMethod]
        public void SanitizeReplacesNaNStandardDeviationOn0()
        {
            MetricTelemetry original = new MetricTelemetry { StandardDeviation = double.NaN };
            ((ITelemetry)original).Sanitize();

            Assert.AreEqual(0, original.StandardDeviation.Value);
        }

        [TestMethod]
        public void SanitizeReplacesNaNSumOn0()
        {
            MetricTelemetry original = new MetricTelemetry();
            original.Name = "Test";
            original.Sum = double.NaN;

            ((ITelemetry)original).Sanitize();

            Assert.AreEqual(0, original.Sum);
        }

        [TestMethod]
        public void SanitizeReplacesNegativeCountOn1()
        {
            MetricTelemetry original = new MetricTelemetry();
            original.Name = "Test";
            original.Count = -5; ;

            ((ITelemetry)original).Sanitize();

            Assert.AreEqual(1, original.Count);
        }

        [TestMethod]
        public void SanitizeReplacesZeroCountOn1()
        {
            MetricTelemetry original = new MetricTelemetry();
            original.Name = "Test";

            ((ITelemetry)original).Sanitize();

            Assert.AreEqual(1, original.Count);
        }

        [TestMethod]
        public void CountPropertyGetterReturnsOneIfNoValueIsSet()
        {
            MetricTelemetry telemetry = new MetricTelemetry();

            Assert.AreEqual(1, telemetry.Count);
        }

#if !NETCOREAPP1_1
        [TestMethod]
        public void MetricTelemetryDeepCloneCopiesAllProperties()
        {
            var metric = new MetricTelemetry();

            metric.Name = "My Page";
#pragma warning disable CS0618
            metric.Value = 42;
#pragma warning restore CS0618
            metric.Count = 5;
            metric.Min = 1.2;
            metric.Max = 6.4;
            metric.StandardDeviation = 0.5;
            metric.Properties.Add("Property1", "Value1");

            MetricTelemetry other = (MetricTelemetry)metric.DeepClone();

            CompareLogic deepComparator = new CompareLogic();
            var comparisonResult = deepComparator.Compare(metric, other);
            Assert.IsTrue(comparisonResult.AreEqual, comparisonResult.DifferencesString);
        }
#endif
    }
}
