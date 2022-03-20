using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformationMatrixUtil
{

	
    /// <summary>
    /// 模型空间的坐标转世界空间坐标
    /// </summary>
    /// <param name="scale">世界空间对模型的缩放</param>
    /// <param name="rotation">世界空间对模型的旋转</param>
    /// <param name="translate">世界空间对模型的平移</param>
    /// <param name="currentPos">模型空间的坐标</param>
    /// <returns></returns>
    public static Vector3 MToWPosition(Vector3 scale, Vector3 rotation, Vector3 translate, Vector3 currentPos)
    {
        Matrix4x4 convertMatrix = MToWMatrix(scale, rotation, translate);
        Vector3 pos= convertMatrix.MultiplyPoint(currentPos);
        return pos;
    }

    /// <summary>
    /// 世界空间的坐标转模型空间坐标
    /// </summary>
    /// <param name="scale">世界空间对模型的缩放</param>
    /// <param name="rotation">世界空间对模型的旋转</param>
    /// <param name="translate">世界空间对模型的平移</param>
    /// <param name="currentPos">世界空间的坐标</param>
    /// <returns></returns>
    public static Vector3 WToMPosition(Vector3 scale, Vector3 rotation, Vector3 translate, Vector3 currentPos)
    {
        Matrix4x4 convertMatrix = MToWMatrix(scale, rotation, translate);
        Vector3 pos = convertMatrix.inverse.MultiplyPoint(currentPos);
        return pos;
    }

    /// <summary>
    /// 模型空间到世界空间的变换矩阵
    /// </summary>
    /// <param name="scale">世界空间对模型空间的缩放</param>
    /// <param name="rotation">世界空间对模型空间的旋转</param>
    /// <param name="translate">世界空间对模型空间的平移</param>
    /// <returns></returns>
    public static Matrix4x4 MToWMatrix(Vector3 scale, Vector3 rotation, Vector3 translate)
    {
        Matrix4x4 scaleMatrix = new Matrix4x4();
        scaleMatrix.SetRow(0, new Vector4(scale.x, 0, 0, 0));
        scaleMatrix.SetRow(1, new Vector4(0, scale.y, 0, 0));
        scaleMatrix.SetRow(2, new Vector4(0, 0, scale.z, 0));
        scaleMatrix.SetRow(3, new Vector4(0, 0, 0, 1));

        rotation *= Mathf.Deg2Rad;
        Matrix4x4 rotateMatrixZ = new Matrix4x4();
        rotateMatrixZ.SetRow(0, new Vector4(Mathf.Cos(rotation.z), -Mathf.Sin(rotation.z), 0, 0));
        rotateMatrixZ.SetRow(1, new Vector4(Mathf.Sin(rotation.z), Mathf.Cos(rotation.z), 0, 0));
        rotateMatrixZ.SetRow(2, new Vector4(0, 0, 1, 0));
        rotateMatrixZ.SetRow(3, new Vector4(0, 0, 0, 1));

        Matrix4x4 rotateMatrixY = new Matrix4x4();
        rotateMatrixY.SetRow(0, new Vector4(Mathf.Cos(rotation.y), 0, Mathf.Sin(rotation.y), 0));
        rotateMatrixY.SetRow(1, new Vector4(0, 1, 0, 0));
        rotateMatrixY.SetRow(2, new Vector4(-Mathf.Sin(rotation.y), 0, Mathf.Cos(rotation.y), 0));
        rotateMatrixY.SetRow(3, new Vector4(0, 0, 0, 1));

        Matrix4x4 rotateMatrixX = new Matrix4x4();
        rotateMatrixX.SetRow(0, new Vector4(1, 0, 0, 0));
        rotateMatrixX.SetRow(1, new Vector4(0, Mathf.Cos(rotation.x), -Mathf.Sin(rotation.x), 0));
        rotateMatrixX.SetRow(2, new Vector4(0, Mathf.Sin(rotation.x), Mathf.Cos(rotation.x), 0));
        rotateMatrixX.SetRow(3, new Vector4(0, 0, 0, 1));

        Matrix4x4 translateMatrix = new Matrix4x4();
        translateMatrix.SetRow(0, new Vector4(1, 0, 0, translate.x));
        translateMatrix.SetRow(1, new Vector4(0, 1, 0, translate.y));
        translateMatrix.SetRow(2, new Vector4(0, 0, 1, translate.z));
        translateMatrix.SetRow(3, new Vector4(0, 0, 0, 1));


        //这里注意顺序，矩阵不满足左右交换的，
        //unity中旋转的顺序是首先绕Z轴进行旋转，然后绕X轴进行旋转，最后绕Y轴进行旋转
        Matrix4x4 convertMatrix = translateMatrix * rotateMatrixY * rotateMatrixX * rotateMatrixZ * scaleMatrix;
        return convertMatrix;
    }


    /// <summary>
    /// 世界空间 转到 观察空间
    /// 观察空间是右手坐标系
    /// </summary>
    /// <param name="worldPos"></param>
    /// <param name="camera"></param>
    /// <returns></returns>
    public static Vector3 WToVPosition(Vector3 worldPos,Camera camera)
    {

        Matrix4x4 mat = WToVMatrix(camera);
        Vector3 viewPos = mat.MultiplyPoint(worldPos);
        return viewPos;
    }

    /// <summary>
    /// 世界空间转观察空间的矩阵
    /// 观察空间是右手坐标系
    /// </summary>
    /// <returns></returns>
    public static Matrix4x4 WToVMatrix(Camera camera)
    {
        //先获取世界空间转模型空间的矩阵（变换矩阵可逆，取逆矩阵）
        Transform camTrans = camera.transform;
        Matrix4x4 matrix = MToWMatrix(camTrans.localScale, camTrans.localEulerAngles, camTrans.position);
        Matrix4x4 inverseMatrix = matrix.inverse;

        //将矩阵改成右手坐标系的矩阵，即z取反
        inverseMatrix.SetRow(2, -inverseMatrix.GetRow(2));
        return inverseMatrix;
    }

    /// <summary>
    /// 观察空间到裁剪空间
    /// 
    /// </summary>
    /// <param name="vPos"></param>
    /// <param name="camera"></param>
    /// <returns></returns>
    public static Vector4 VToPPosition(Vector3 vPos,Camera camera)
    {

        Matrix4x4 matrix = VToPMatrix(camera);
        Vector4 p = matrix*new Vector4( vPos.x,vPos.y,vPos.z,1);
        return p;
    }

    /// <summary>
    /// 观察空间到裁剪空间的变换矩阵
    /// </summary>
    /// <returns></returns>
    public static Matrix4x4 VToPMatrix(Camera camera)
    {
        Camera cam = camera;
        float near = cam.nearClipPlane;
        float far = cam.farClipPlane;
        float fov = cam.fieldOfView;
        float aspect = cam.aspect;
        Matrix4x4 matrix = VToPMatrix(fov, near, far, aspect);
        return matrix;
    }
    /// <summary>
    /// 透视裁剪矩阵
    /// 透视投影矩阵
    /// 参考（https://blog.csdn.net/cbbbc/article/details/51296804）
    /// View to Projection
    /// 观察空间到裁剪空间
    /// </summary>
    /// <param name="fov"></param>
    /// <param name="near"></param>
    /// <param name="far"></param>
    /// <param name="aspect"></param>
    /// <returns></returns>
    public static Matrix4x4 VToPMatrix(float fov,float near,float far,float aspect)
    {
        float tan= Mathf.Tan((fov * Mathf.Deg2Rad) / 2);

        Matrix4x4 matrix = new Matrix4x4();
        matrix.SetRow(0, new Vector4(1 / (aspect * tan), 0, 0, 0));
        matrix.SetRow(1, new Vector4(0,1 / (tan), 0, 0));
        matrix.SetRow(2, new Vector4(0, 0, -(far + near) / (far - near), -2 * far * near / (far - near)));
        matrix.SetRow(3, new Vector4(0, 0, -1, 0));

        return matrix;
    }

    /// <summary>
    /// 裁剪空间到屏幕空间的坐标变换（x,y是有效的）
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public static Vector4 PToScreenPosition(Vector4 p)
    {
        Vector4 ndcPos= PToNdcPosition(p);
        Vector4 texPos = NdcToTexturePosition(ndcPos);
        Vector4 screenPos = TextureToScreenPosition(texPos);
        return screenPos;
    }


    /// <summary>
    /// NDC是一个归一化的空间，坐标空间范围为[-1, 1]。从Projection空间到NDC空间的做法就是做了一个齐次除法！
    /// Projection to NDC
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public static Vector4 PToNdcPosition(Vector4 p)
    {
        return p / p.w;
    }

    /// <summary>
    /// NDC - Texture Space
    /// (NDC - Viewport Space 视口空间,z的值这里和视口空间不一样，z的范围是[-1,1],视口坐标的z值是实际的z值)
    /// 这个过程是将[-1, 1]映射到[0, 1]之间。
    /// NDC to Texture space
    /// </summary>
    /// <param name="ndcPoint"></param>
    /// <returns></returns>
    public static Vector4 NdcToTexturePosition(Vector4 ndcPoint)
    {
        return (ndcPoint + Vector4.one) / 2;
    }

    /// <summary>
    /// Texture Space - Screen Space
    /// 这个过程就是得到顶点最终在屏幕上的坐标，其实就是利用Texture Space的坐标乘上屏幕的宽高。
    /// Texture to Screen
    /// </summary>
    /// <param name="texturePoint"></param>
    /// <returns></returns>
    public static Vector4 TextureToScreenPosition(Vector4 texturePoint)
    {
        Vector4 screenPos = new Vector4(texturePoint.x * Screen.width, texturePoint.y * Screen.height, texturePoint.z, texturePoint.w);
        return screenPos;
    }

}