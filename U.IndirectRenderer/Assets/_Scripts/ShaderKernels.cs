using UnityEngine;

public static class ShaderKernels
{
    public static int MatricesInitializer => _matricesInitializer;
    public static int LodSorter => _lodSorter;
    public static int LodTransposedSorter => _lodTransposedSorter;
    public static int Culler => _culler;
    public static int InstancesScanner => _instancesScanner;
    public static int GroupSumsScanner => _groupSumsScanner;
    public static int InstancesDataCopier => _instancesDataCopier;

    private static int _matricesInitializer = -1;
    private static int _lodSorter = -1;
    private static int _lodTransposedSorter = -1;
    private static int _culler = -1;
    private static int _instancesScanner = -1;
    private static int _groupSumsScanner = -1;
    private static int _instancesDataCopier = -1;

    public static void Initialize(IndirectRendererConfig config)
    {
        TryGetKernel("CSMain",          config.MatricesInitializer, out _matricesInitializer);
        TryGetKernel("BitonicSort",     config.LodBitonicSorter,    out _lodSorter);
        TryGetKernel("MatrixTranspose", config.LodBitonicSorter,    out _lodTransposedSorter);
        TryGetKernel("CSMain",          config.InstancesCuller,     out _culler);
        TryGetKernel("CSMain",          config.InstancesScanner,    out _instancesScanner);
        TryGetKernel("CSMain",          config.GroupSumsScanner,    out _groupSumsScanner);
        // TryGetKernel("CSMain", _instancesDataCopier, out _instancesDataCopier);
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