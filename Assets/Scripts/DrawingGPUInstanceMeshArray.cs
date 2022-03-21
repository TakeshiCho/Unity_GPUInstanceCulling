using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class DrawingGPUInstanceMeshArray : MonoBehaviour
{

    public ComputeShader computeShader;
    private InstanceDataBuffer _buffer;
    private uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };
    private int _kernelOfComupteIndex2D;
    private int _kernelOfComputeMeshData;
    private int _kernelOfComupteCulling;
    private static readonly int InstanceBuffer = Shader.PropertyToID("_InstanceBuffer");

    public new Camera camera;
    private float _cameraHalfDiagonalAnglesDotProduct;
    public bool checkSingleMeshPosition;

    [Space]
    public int length = 512;
    public float interval = 1.5f;

    public Mesh mesh;
    public Shader shader;
    private Material _material;
    private Bounds _bounds;
    private Vector3 _boundsCenter;
    private Vector3 _boundsSize;
    private float3 _singleInMeshSize;
    private int _count;

    public float cullingDistacne = 10f;
    
    struct MeshArrayData
    {
        public float3 Position;
        public float4 Color;
    }
    struct IndirectArgumentData
    {
        public uint IndexCountPerInstance;
        public uint InstanceCount;
        public uint StartIndexLocation;
        public uint BaseVertexLocation;
        public uint StartInstanceLocation;
    }

    struct InstanceDataBuffer
    {
        public ComputeBuffer Index2D;
        public ComputeBuffer ArgBuffer;
        public ComputeBuffer Origin;
        public ComputeBuffer Culling;
    }


    private void Awake()
    {
        _singleInMeshSize = mesh.bounds.size;
        _material = new Material(shader);
        _count = length * length;
        _boundsCenter = math.float3(0,0,0);
        _boundsSize = math.float3((length - 1) * interval+_singleInMeshSize.x, _singleInMeshSize.y, (length - 1) * interval+_singleInMeshSize.z);
        _bounds = new Bounds(_boundsCenter,_boundsSize);

        _buffer = new InstanceDataBuffer();
        _buffer.Index2D = new ComputeBuffer(_count, Marshal.SizeOf(typeof(float2)), ComputeBufferType.Structured);
        _buffer.ArgBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        _buffer.Origin = new ComputeBuffer(_count,Marshal.SizeOf(typeof(MeshArrayData)),ComputeBufferType.Structured);
        _buffer.Culling = new ComputeBuffer(_count,Marshal.SizeOf(typeof(MeshArrayData)),ComputeBufferType.Append);
        _args[0] = (uint)mesh.GetIndexCount(0);
        _buffer.ArgBuffer.SetData(_args);
        
        _kernelOfComupteIndex2D = computeShader.FindKernel("ComputeIndex2D");
        _kernelOfComputeMeshData = computeShader.FindKernel("ComputeMeshData");
        _kernelOfComupteCulling = computeShader.FindKernel("ComputeCullingUpdate");
        _cameraHalfDiagonalAnglesDotProduct = GetCameraHalfDiagonalAnglesDotProduct(camera);
    }

    private void Start()
    {
        computeShader.SetInt("_Length",length);
        computeShader.SetFloat("_Interval",interval);
        computeShader.SetBool("_IsCheckPosition",checkSingleMeshPosition);
        computeShader.SetFloat("_CameraHaflDiagonalAngleDotProductor",_cameraHalfDiagonalAnglesDotProduct);

        computeShader.SetBuffer(_kernelOfComupteIndex2D,"Index2D",_buffer.Index2D);
        computeShader.Dispatch(_kernelOfComupteIndex2D, _count / 16, 1, 1);
        
        computeShader.SetBuffer(_kernelOfComputeMeshData,"Index2D",_buffer.Index2D);
        computeShader.SetBuffer(_kernelOfComputeMeshData,"OringinalDataBuffer",_buffer.Origin);
        computeShader.Dispatch(_kernelOfComputeMeshData, _count / 16, 1, 1);
        
        computeShader.SetBuffer(_kernelOfComupteCulling,"Index2D",_buffer.Index2D);
        computeShader.SetBuffer(_kernelOfComupteCulling,"ArgBuffer",_buffer.ArgBuffer);
        computeShader.SetBuffer(_kernelOfComupteCulling,"CullBuffer",_buffer.Culling);
        computeShader.SetBuffer(_kernelOfComupteCulling,"OringinalDataBuffer",_buffer.Origin);

        Shader.SetGlobalBuffer(InstanceBuffer,_buffer.Culling);
    }
    
    void Update()
    {
        _buffer.Culling.SetCounterValue(0);
        computeShader.SetFloat("_CullingDistacne",cullingDistacne);
        computeShader.SetVector("_CameraPosition",camera.gameObject.transform.position);
        computeShader.SetVector("_CameraForward",camera.transform.forward);
        computeShader.Dispatch(_kernelOfComupteCulling, _count / 16, 1, 1);
        ComputeBuffer.CopyCount(_buffer.Culling,_buffer.ArgBuffer,sizeof(uint));
        Graphics.DrawMeshInstancedIndirect(mesh,0,_material,_bounds,_buffer.ArgBuffer);
    }

    float GetCameraHalfDiagonalAnglesDotProduct(Camera cam)
    {
        float ratio = (float)Screen.width / Screen.height;
        float camVerticalFov = cam.fieldOfView * Mathf.Deg2Rad;
        float camFarDistance = cam.farClipPlane;
        float camFarHeight = camFarDistance * math.tan(camVerticalFov * 0.5f) * 2;
        float camFarWidth = camFarHeight * ratio;
        float camFarDiagonal = math.sqrt(camFarHeight * camFarHeight + camFarWidth * camFarWidth);
        float camFarDiagonalHalf = camFarDiagonal * 0.5f;
        float camHypotenuse = math.sqrt(camFarDistance * camFarDistance + camFarDiagonalHalf * camFarDiagonalHalf);
        float coshalfCamDiagonalAngle = camFarDistance / camHypotenuse;
        return coshalfCamDiagonalAngle;
    }

    private void OnDestroy()
    {
        _buffer.Index2D.Dispose();
        _buffer.ArgBuffer.Dispose();
        _buffer.Origin.Dispose();
        _buffer.Culling.Dispose();
        Destroy(_material);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(_boundsCenter,_boundsSize);
    }
    
    [ContextMenu("Check Mesh Count")]
    private void CheckMeshCount()
    {
        IndirectArgumentData[] data = new IndirectArgumentData[1];
        _buffer.ArgBuffer.GetData(data);
        Debug.LogFormat("Mesh:{0} Vertex:{1}", data[0].InstanceCount, data[0].InstanceCount * mesh.vertexCount);
    }
}
