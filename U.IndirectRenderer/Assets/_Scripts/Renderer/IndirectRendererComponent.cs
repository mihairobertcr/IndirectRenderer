using UnityEngine;

public class IndirectRendererComponent : MonoBehaviour
{
    [SerializeField] private Camera _renderCamera;
    [SerializeField] private RendererConfig _config;
    [SerializeField] private MeshesCollection _meshes;

    private IndirectRenderer _renderer;

    private void Start()
    {
        foreach (var instance in _meshes.Data)
        {
            instance.Transforms.Clear();
            for (var i = 0; i < 128; i++)
            {
                for (var j = 0; j < 128; j++)
                {
                    var data = new MeshProperties.TransformDto
                    {
                        Position = new Vector3
                        {
                            x = i,
                            y = .5f,
                            z = j
                        },
                        
                        Rotation = new Vector3
                        {
                            x = 0f,
                            y = 0f,
                            z = 0f
                        },
                        
                        Scale = new Vector3
                        {
                            x = .75f,
                            y = .75f,
                            z = .75f
                        }
                    };
        
                    //TODO: Move to MeshProperties
                    data.Position += instance.Offset.Position;
                    data.Rotation += instance.Offset.Rotation;
                    data.Scale += instance.Offset.Scale;
                    
                    instance.Transforms.Add(data);
                }
            }
        }

        _renderer = new IndirectRenderer(_renderCamera, _config, _meshes.Data);
    }

    private void OnDestroy()
    {
        _renderer.Dispose();
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        
        _renderer.DrawGizmos();
    }
}
