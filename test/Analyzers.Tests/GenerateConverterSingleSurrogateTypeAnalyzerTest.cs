using Microsoft.CodeAnalysis;
using Orleans.Analyzers;
using Xunit;

namespace Analyzers.Tests;

[TestCategory("BVT"), TestCategory("Analyzer")]
public class GenerateConverterSingleSurrogateTypeAnalyzerTest : DiagnosticAnalyzerTestBase<GenerateConverterSingleSurrogateTypeAnalyzer>
{

    [Fact]
    public async Task GenerateErrorWhenDeclaringMultipleConverters()
    {
        var code = """
                    public class ForeignType1
                    {
                        public string String { get; set; }
                    }

                    public class ForeignType2
                    {
                        public int Int { get; set; }
                    }

                    [GenerateSerializer]
                    public struct ForeignType1Surrogate
                    {
                        [Id(0)]
                        public string String { get; set; }
                    }
                    
                    [GenerateSerializer]
                    public struct ForeignType2Surrogate
                    {
                        [Id(0)]
                        public string String { get; set; }
                    }

                    [RegisterConverter]
                    public class ForeignTypesConverter
                        :IConverter<ForeignType1, ForeignType1Surrogate>
                        ,IConverter<ForeignType2, ForeignType2Surrogate>
                    {
                        public ForeignType1 ConvertFromSurrogate(in ForeignType1Surrogate surrogate)
                            => throw new NotImplementedException();

                        public ForeignType1Surrogate ConvertToSurrogate(in ForeignType1 value)
                            => throw new NotImplementedException();

                        public ForeignType2 ConvertFromSurrogate(in ForeignType2Surrogate surrogate)
                            => throw new NotImplementedException();
                    
                        public ForeignType2Surrogate ConvertToSurrogate(in ForeignType2 value)
                            => throw new NotImplementedException();
                    }
                    """;

        var (diagnostics, _) = await GetDiagnosticsAsync(code, new string[0]);

        Assert.NotEmpty(diagnostics);
        Assert.Single(diagnostics);

        var diagnostic = diagnostics.First();
        Assert.Equal(GenerateConverterSingleSurrogateTypeAnalyzer.RuleId, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public async Task DoesNotGenerateErrorWhenDeclaringSingleConverter()
    {
        var code = """
                    public class ForeignType1
                    {
                        public string String { get; set; }
                    }

                    [GenerateSerializer]
                    public struct ForeignType1Surrogate
                    {
                        [Id(0)]
                        public string String { get; set; }
                    }

                    [RegisterConverter]
                    public class ForeignType1Converter
                        :IConverter<ForeignType1, ForeignType1Surrogate>
                    {
                        public ForeignType1 ConvertFromSurrogate(in ForeignType1Surrogate surrogate)
                            => throw new NotImplementedException();

                        public ForeignType1Surrogate ConvertToSurrogate(in ForeignType1 value)
                            => throw new NotImplementedException();
                    }
                    """;

        var (diagnostics, _) = await GetDiagnosticsAsync(code, new string[0]);

        Assert.Empty(diagnostics);
    }
}
