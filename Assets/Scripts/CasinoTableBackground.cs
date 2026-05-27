using UnityEngine;

/// <summary>
/// Draws a procedural blackjack table at runtime - no sprite assets needed.
/// Generates a texture with felt color, an oval table outline, the classic
/// "BLACKJACK PAYS 3 TO 2" arc and dealer rules text, then renders it as a
/// full-screen background.
/// </summary>
[ExecuteAlways]
public class CasinoTableBackground : MonoBehaviour
{
    [Header("Camera")]
    public Camera targetCamera;

    [Header("Colors")]
    public Color floorColor   = new Color(0.10f, 0.05f, 0.03f, 1f); // dark wood
    public Color feltColor    = new Color(0.05f, 0.35f, 0.15f, 1f); // casino green
    public Color rimColor     = new Color(0.25f, 0.10f, 0.04f, 1f); // mahogany rim
    public Color stripeColor  = new Color(0.90f, 0.75f, 0.30f, 1f); // gold stripe
    public Color textColor    = new Color(0.95f, 0.85f, 0.40f, 1f); // gold text

    [Header("Layout")]
    [Tooltip("Resolution of the generated texture. Higher = sharper.")]
    public int textureWidth  = 2048;
    public int textureHeight = 1152;

    [Header("Sorting")]
    public int sortingOrder = -100;

    private SpriteRenderer _renderer;
    private Texture2D _texture;

    void OnEnable()  { Build(); }
    void OnDisable() { Cleanup(); }
    void OnValidate(){ if (isActiveAndEnabled) Build(); }

    void Build()
    {
        if (targetCamera == null) targetCamera = Camera.main;

        if (_renderer == null)
        {
            Transform existing = transform.Find("TableSprite");
            GameObject go = existing != null ? existing.gameObject : new GameObject("TableSprite");
            go.transform.SetParent(transform, false);
            _renderer = go.GetComponent<SpriteRenderer>();
            if (_renderer == null) _renderer = go.AddComponent<SpriteRenderer>();
        }
        _renderer.sortingOrder = sortingOrder;

        if (_texture == null || _texture.width != textureWidth || _texture.height != textureHeight)
        {
            if (_texture != null) DestroyImmediate(_texture);
            _texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            _texture.hideFlags = HideFlags.HideAndDontSave;
        }

        PaintTable(_texture);

        Sprite sprite = Sprite.Create(
            _texture,
            new Rect(0, 0, _texture.width, _texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        _renderer.sprite = sprite;

        FitToCamera();
    }

    void Cleanup()
    {
        if (_texture != null) { DestroyImmediate(_texture); _texture = null; }
    }

    void FitToCamera()
    {
        if (_renderer == null || _renderer.sprite == null || targetCamera == null) return;
        float worldH = targetCamera.orthographic ? targetCamera.orthographicSize * 2f : 10f;
        float worldW = worldH * targetCamera.aspect;
        Vector2 b = _renderer.sprite.bounds.size;
        if (b.x <= 0 || b.y <= 0) return;
        _renderer.transform.localPosition = new Vector3(0, 0, 0.1f);
        _renderer.transform.localScale = new Vector3(worldW / b.x, worldH / b.y, 1f);
    }

    // ----- painting -----

    void PaintTable(Texture2D tex)
    {
        int W = tex.width;
        int H = tex.height;
        Color[] px = new Color[W * H];

        // Table is an ellipse (half-table look) centered at the bottom.
        // We tilt it slightly: wide horizontally, shorter vertically.
        float cx = W * 0.5f;
        float cy = H * 0.32f;        // table center pushed below screen middle
        float rx = W * 0.48f;        // horizontal radius
        float ry = H * 0.62f;        // vertical radius

        float rimWidth     = Mathf.Min(W, H) * 0.05f;
        float stripeWidth  = Mathf.Min(W, H) * 0.008f;
        float stripeOffset = rimWidth + stripeWidth * 2f;

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float dx = (x - cx) / rx;
                float dy = (y - cy) / ry;
                float r = Mathf.Sqrt(dx * dx + dy * dy); // 0 at center, 1 at table edge

                Color c;
                if (r > 1f)
                {
                    // outside table
                    c = floorColor;
                }
                else
                {
                    // inside table - figure out felt vs rim vs stripe
                    // distance from edge in pixels (approx along normal)
                    float edgeDistPx = (1f - r) * Mathf.Min(rx, ry);

                    if (edgeDistPx < rimWidth)                          c = rimColor;
                    else if (edgeDistPx < rimWidth + stripeWidth)       c = stripeColor;
                    else if (edgeDistPx < stripeOffset)                 c = rimColor;
                    else if (edgeDistPx < stripeOffset + stripeWidth)   c = stripeColor;
                    else                                                c = feltColor;
                }

                px[y * W + x] = c;
            }
        }

        // Subtle radial vignette on the felt for depth.
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                int i = y * W + x;
                if (px[i] != feltColor) continue;
                float dx = (x - cx) / rx;
                float dy = (y - cy) / ry;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float darken = Mathf.Lerp(1.15f, 0.75f, r);
                px[i] = new Color(
                    Mathf.Clamp01(feltColor.r * darken),
                    Mathf.Clamp01(feltColor.g * darken),
                    Mathf.Clamp01(feltColor.b * darken),
                    1f);
            }
        }

        tex.SetPixels(px);

        // Curved gold text along the upper arc of the felt (sin > 0 = top in tex space).
        DrawTextOnArc(tex, "BLACKJACK  PAYS  3  TO  2",
            cx, cy, rx * 0.82f, ry * 0.82f,
            startAngleDeg: 160f, endAngleDeg: 20f,
            charPx: Mathf.RoundToInt(H * 0.045f),
            color: textColor);

        // Straight tagline lower.
        DrawTextCentered(tex, "DEALER  MUST  DRAW  TO  16  AND  STAND  ON  ALL  17'S",
            (int)cx, (int)(cy + ry * 0.05f),
            charPx: Mathf.RoundToInt(H * 0.025f),
            color: textColor);

        tex.Apply();
    }

    // ----- tiny built-in pixel font -----

    void DrawTextCentered(Texture2D tex, string text, int cx, int cy, int charPx, Color color)
    {
        int totalW = text.Length * charPx;
        int x = cx - totalW / 2;
        int y = cy - charPx / 2;
        foreach (char ch in text)
        {
            DrawChar(tex, ch, x, y, charPx, color);
            x += charPx;
        }
    }

    void DrawTextOnArc(Texture2D tex, string text, float cx, float cy, float rx, float ry,
                       float startAngleDeg, float endAngleDeg, int charPx, Color color)
    {
        if (text.Length == 0) return;
        for (int i = 0; i < text.Length; i++)
        {
            float t = (text.Length == 1) ? 0.5f : (float)i / (text.Length - 1);
            float ang = Mathf.Lerp(startAngleDeg, endAngleDeg, t) * Mathf.Deg2Rad;
            int x = Mathf.RoundToInt(cx + Mathf.Cos(ang) * rx) - charPx / 2;
            int y = Mathf.RoundToInt(cy + Mathf.Sin(ang) * ry) - charPx / 2;
            DrawChar(tex, text[i], x, y, charPx, color);
        }
    }

    // 5x7 pixel font for A-Z 0-9 space and a few punctuation marks.
    static readonly System.Collections.Generic.Dictionary<char, string[]> Font = new()
    {
        { ' ', new[]{ "00000","00000","00000","00000","00000","00000","00000" } },
        { 'A', new[]{ "01110","10001","10001","11111","10001","10001","10001" } },
        { 'B', new[]{ "11110","10001","10001","11110","10001","10001","11110" } },
        { 'C', new[]{ "01110","10001","10000","10000","10000","10001","01110" } },
        { 'D', new[]{ "11110","10001","10001","10001","10001","10001","11110" } },
        { 'E', new[]{ "11111","10000","10000","11110","10000","10000","11111" } },
        { 'F', new[]{ "11111","10000","10000","11110","10000","10000","10000" } },
        { 'G', new[]{ "01110","10001","10000","10111","10001","10001","01110" } },
        { 'H', new[]{ "10001","10001","10001","11111","10001","10001","10001" } },
        { 'I', new[]{ "11111","00100","00100","00100","00100","00100","11111" } },
        { 'J', new[]{ "00111","00010","00010","00010","00010","10010","01100" } },
        { 'K', new[]{ "10001","10010","10100","11000","10100","10010","10001" } },
        { 'L', new[]{ "10000","10000","10000","10000","10000","10000","11111" } },
        { 'M', new[]{ "10001","11011","10101","10101","10001","10001","10001" } },
        { 'N', new[]{ "10001","10001","11001","10101","10011","10001","10001" } },
        { 'O', new[]{ "01110","10001","10001","10001","10001","10001","01110" } },
        { 'P', new[]{ "11110","10001","10001","11110","10000","10000","10000" } },
        { 'Q', new[]{ "01110","10001","10001","10001","10101","10010","01101" } },
        { 'R', new[]{ "11110","10001","10001","11110","10100","10010","10001" } },
        { 'S', new[]{ "01111","10000","10000","01110","00001","00001","11110" } },
        { 'T', new[]{ "11111","00100","00100","00100","00100","00100","00100" } },
        { 'U', new[]{ "10001","10001","10001","10001","10001","10001","01110" } },
        { 'V', new[]{ "10001","10001","10001","10001","10001","01010","00100" } },
        { 'W', new[]{ "10001","10001","10001","10101","10101","10101","01010" } },
        { 'X', new[]{ "10001","10001","01010","00100","01010","10001","10001" } },
        { 'Y', new[]{ "10001","10001","01010","00100","00100","00100","00100" } },
        { 'Z', new[]{ "11111","00001","00010","00100","01000","10000","11111" } },
        { '0', new[]{ "01110","10001","10011","10101","11001","10001","01110" } },
        { '1', new[]{ "00100","01100","00100","00100","00100","00100","01110" } },
        { '2', new[]{ "01110","10001","00001","00010","00100","01000","11111" } },
        { '3', new[]{ "11110","00001","00001","01110","00001","00001","11110" } },
        { '4', new[]{ "00010","00110","01010","10010","11111","00010","00010" } },
        { '5', new[]{ "11111","10000","11110","00001","00001","10001","01110" } },
        { '6', new[]{ "01110","10000","10000","11110","10001","10001","01110" } },
        { '7', new[]{ "11111","00001","00010","00100","01000","10000","10000" } },
        { '8', new[]{ "01110","10001","10001","01110","10001","10001","01110" } },
        { '9', new[]{ "01110","10001","10001","01111","00001","00001","01110" } },
        { '\'',new[]{ "00100","00100","01000","00000","00000","00000","00000" } },
    };

    void DrawChar(Texture2D tex, char ch, int x, int y, int charPx, Color color)
    {
        char up = char.ToUpperInvariant(ch);
        if (!Font.TryGetValue(up, out string[] glyph)) glyph = Font[' '];

        int cellW = Mathf.Max(1, charPx / 6);    // pixel size for each "dot"
        int rows = 7, cols = 5;

        for (int gy = 0; gy < rows; gy++)
        {
            string row = glyph[gy];
            for (int gx = 0; gx < cols; gx++)
            {
                if (row[gx] != '1') continue;
                int px0 = x + gx * cellW;
                int py0 = y + (rows - 1 - gy) * cellW; // flip Y - texture origin is bottom-left

                for (int dy = 0; dy < cellW; dy++)
                {
                    for (int dx = 0; dx < cellW; dx++)
                    {
                        int tx = px0 + dx;
                        int ty = py0 + dy;
                        if (tx < 0 || ty < 0 || tx >= tex.width || ty >= tex.height) continue;
                        tex.SetPixel(tx, ty, color);
                    }
                }
            }
        }
    }
}
