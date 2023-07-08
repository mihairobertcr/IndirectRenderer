using UnityEngine;

public static class ShaderKernels
{
    public static int MatricesInitializer => _matricesInitializer;
    public static int LodSorter => _lodSorter;
    public static int LodTransposedSorter => _lodTransposedSorter;
    public static int InstancesCuller => _instancesCuller;
    public static int InstancesScanner => _instancesScanner;
    public static int GroupSumsScanner => _groupSumsScanner;
    public static int DataCopier => _dataCopier;
    public static int ArgumentsSplitter => _argumentsSplitter;

    private static int _matricesInitializer = -1;
    private static int _lodSorter = -1;
    private static int _lodTransposedSorter = -1;
    private static int _instancesCuller = -1;
    private static int _instancesScanner = -1;
    private static int _groupSumsScanner = -1;
    private static int _dataCopier = -1;
    private static int _argumentsSplitter = -1;

    public static void Initialize(IndirectRendererConfig config)
    {
        TryGetKernel("CSMain",          config.MatricesInitializer, out _matricesInitializer);
        TryGetKernel("BitonicSort",     config.LodBitonicSorter,    out _lodSorter);
        TryGetKernel("MatrixTranspose", config.LodBitonicSorter,    out _lodTransposedSorter);
        TryGetKernel("CSMain",          config.InstancesCuller,     out _instancesCuller);
        TryGetKernel("CSMain",          config.InstancesScanner,    out _instancesScanner);
        TryGetKernel("CSMain",          config.GroupSumsScanner,    out _groupSumsScanner);
        TryGetKernel("CSMain",          config.InstancesDataCopier, out _dataCopier);
        TryGetKernel("SplitArguments",  config.InstancesDataCopier, out _argumentsSplitter);
    }

    private static bool TryGetKernel(string kernelName, ComputeShader computeShader, out int kernelId)
    {
        kernelId = default;
        if (!computeShader.HasKernel(kernelName))
        {
            Debug.LogError($"{kernelName} kernel not found in {computeShader.name}!");
            return false;
        }

        kernelId = computeShader.FindKernel(kernelName);
        return true;
    }
}