using UnityEngine;
using Keensight.Rendering.Context;

namespace Keensight.Rendering.ShaderDispatchers
{
    public class GroupSumsScanningDispatcher : ComputeShaderDispatcher
    {
        private static readonly int GroupsCountId = Shader.PropertyToID("_GroupsCount");
        private static readonly int GroupSumsInputId = Shader.PropertyToID("_GroupSumsInput");
        private static readonly int GroupSumsOutputId = Shader.PropertyToID("_GroupSumsOutput");
        
        private readonly int _kernel;
        
        private readonly ComputeBuffer _groupSumsBuffer;
        private readonly ComputeBuffer _scannedGroupSumsBuffer;

        public GroupSumsScanningDispatcher(RendererContext context)
            : base(context.Config.GroupSumsScanning, context)
        {
            _kernel = GetKernel("CSMain");
            InitializeGroupSumsBuffers(out _groupSumsBuffer, out _scannedGroupSumsBuffer);
        }

        public override ComputeShaderDispatcher Initialize()
        {
            SubmitGroupCount();
            SubmitGroupSumsData();
            
            return this;
        }

        public override void Dispatch() => ComputeShader.Dispatch(_kernel, 1, 1, 1);
        
        private void SubmitGroupCount() => ComputeShader.SetInt(GroupsCountId, Context.MeshesCount / (2 * SCAN_THREAD_GROUP_SIZE));

        private void SubmitGroupSumsData()
        {
            ComputeShader.SetBuffer(_kernel, GroupSumsInputId, _groupSumsBuffer);
            ComputeShader.SetBuffer(_kernel, GroupSumsOutputId, _scannedGroupSumsBuffer);
        }

        private void InitializeGroupSumsBuffers(out ComputeBuffer groupSums, out ComputeBuffer scannedGroupSums)
        {
            groupSums = Context.GroupSums;
            scannedGroupSums = Context.ScannedGroupSums;
        }
    }
}