using System;
using System.Collections.Generic;
using System.Linq;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Bindings.Reflection;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Tracing;
using TechTalk.SpecFlow.UnitTestProvider;

namespace TechTalk.SpecFlow.ErrorHandling
{
    public interface IErrorProvider
    {
        string GetMethodText(IBindingMethod method);
        Exception GetCallError(IBindingMethod method, Exception ex);
        Exception GetParameterCountError(BindingMatch match, int expectedParameterCount);
        Exception GetAmbiguousMatchError(List<BindingMatch> matches, StepInstance stepInstance);
        Exception GetAmbiguousBecauseParamCheckMatchError(List<BindingMatch> matches, StepInstance stepInstance);
        Exception GetNoMatchBecauseOfScopeFilterError(List<BindingMatch> matches, StepInstance stepInstance);
        MissingStepDefinitionException GetMissingStepDefinitionError();
        PendingStepException GetPendingStepDefinitionError();
        void ThrowPendingError(ScenarioExecutionStatus testStatus, string message);
        Exception GetTooManyBindingParamError(int maxParam);
        Exception GetNonStaticEventError(IBindingMethod method);
        Exception GetObsoleteStepError(BindingObsoletion bindingObsoletion);
    }

    internal class ErrorProvider : IErrorProvider
    {
        private readonly IStepFormatter stepFormatter;
        private readonly IUnitTestRuntimeProvider unitTestRuntimeProvider;
        private readonly Configuration.SpecFlowConfiguration specFlowConfiguration;

        public ErrorProvider(IStepFormatter stepFormatter, Configuration.SpecFlowConfiguration specFlowConfiguration, IUnitTestRuntimeProvider unitTestRuntimeProvider)
        {
            this.stepFormatter = stepFormatter;
            this.unitTestRuntimeProvider = unitTestRuntimeProvider;
            this.specFlowConfiguration = specFlowConfiguration;
        }

        public string GetMethodText(IBindingMethod method)
        {
            string parametersDisplayed = string.Join(", ", method.Parameters.Select(p => p.Type.Name).ToArray());
            return $"{method.Type.AssemblyName}:{method.Type.FullName}.{method.Name}({parametersDisplayed})";
        }

        public Exception GetCallError(IBindingMethod method, Exception ex)
        {
            return new BindingException($"Error calling binding method '{GetMethodText(method)}': {ex.Message}");
        }

        public Exception GetParameterCountError(BindingMatch match, int expectedParameterCount)
        {
            return new BindingException(
                $"Parameter count mismatch! The binding method '{GetMethodText(match.StepBinding.Method)}' should have {expectedParameterCount} parameters");
        }

        public Exception GetAmbiguousMatchError(List<BindingMatch> matches, StepInstance stepInstance)
        {
            string stepDescription = stepFormatter.GetStepDescription(stepInstance);
            return new BindingException(
                $"Ambiguous step definitions found for step '{stepDescription}': {string.Join(", ", matches.Select(m => GetMethodText(m.StepBinding.Method)).ToArray())}");
        }


        public Exception GetAmbiguousBecauseParamCheckMatchError(List<BindingMatch> matches, StepInstance stepInstance)
        {
            string stepDescription = stepFormatter.GetStepDescription(stepInstance);
            return new BindingException(
                "Multiple step definitions found, but none of them have matching parameter count and type for step "
                + $"'{stepDescription}': {string.Join(", ", matches.Select(m => GetMethodText(m.StepBinding.Method)).ToArray())}");
        }

        public Exception GetNoMatchBecauseOfScopeFilterError(List<BindingMatch> matches, StepInstance stepInstance)
        {
            string stepDescription = stepFormatter.GetStepDescription(stepInstance);
            return new BindingException(
                "Multiple step definitions found, but none of them have matching scope for step "
                + $"'{stepDescription}': {string.Join(", ", matches.Select(m => GetMethodText(m.StepBinding.Method)).ToArray())}");
        }

        public MissingStepDefinitionException GetMissingStepDefinitionError()
        {
            return new MissingStepDefinitionException();
        }

        public PendingStepException GetPendingStepDefinitionError()
        {
            return new PendingStepException();
        }

        public void ThrowPendingError(ScenarioExecutionStatus testStatus, string message)
        {
            switch (specFlowConfiguration.MissingOrPendingStepsOutcome)
            {
                case MissingOrPendingStepsOutcome.Pending:
                    unitTestRuntimeProvider.TestPending(message);
                    break;
                case MissingOrPendingStepsOutcome.Inconclusive:
                    unitTestRuntimeProvider.TestInconclusive(message);
                    break;
                case MissingOrPendingStepsOutcome.Ignore:
                    unitTestRuntimeProvider.TestIgnore(message);
                    break;
                default:
                    if (testStatus == ScenarioExecutionStatus.UndefinedStep)
                        throw GetMissingStepDefinitionError();
                    throw GetPendingStepDefinitionError();
            }

        }

        public Exception GetTooManyBindingParamError(int maxParam)
        {
            return new BindingException($"Binding methods with more than {maxParam} parameters are not supported");
        }

        public Exception GetNonStaticEventError(IBindingMethod method)
        {
            throw new BindingException($"The binding methods for before/after feature and before/after test run events must be static! {GetMethodText(method)}");
        }

        public Exception GetObsoleteStepError(BindingObsoletion bindingObsoletion)
        {
            throw new BindingException(bindingObsoletion.Message);
        }
    }
}