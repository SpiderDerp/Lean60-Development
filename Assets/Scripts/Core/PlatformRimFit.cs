using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[ExecuteAlways]
public class PlatformRimFit : MonoBehaviour
{
    static readonly int ObjectScaleId = Shader.PropertyToID("_ObjectScale");

    SpriteRenderer _sr;
    MaterialPropertyBlock _block;

    void OnEnable()
    {
        _sr = GetComponent<SpriteRenderer>();
        _block = new MaterialPropertyBlock();
        Apply();
    }

    void LateUpdate()
    {
        Apply();
    }

    void Apply()
    {
        if (_sr == null)
            return;

        if (_block == null)
            _block = new MaterialPropertyBlock();

        Vector3 lossy = transform.lossyScale;
        Vector2 spriteSize = Vector2.one;
        if (_sr.sprite != null)
        {
            Vector3 b = _sr.sprite.bounds.size;
            spriteSize = new Vector2(b.x, b.y);
        }

        _sr.GetPropertyBlock(_block);
        _block.SetVector(ObjectScaleId, new Vector4(
            Mathf.Abs(lossy.x) * spriteSize.x,
            Mathf.Abs(lossy.y) * spriteSize.y,
            1f,
            0f));
        _sr.SetPropertyBlock(_block);
    }
}
