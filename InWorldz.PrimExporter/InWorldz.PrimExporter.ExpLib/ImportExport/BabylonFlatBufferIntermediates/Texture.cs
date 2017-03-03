namespace InWorldz.PrimExporter.ExpLib.ImportExport.BabylonFlatBufferIntermediates
{
    /// <summary>
    /// Intermediate output for babylon flatbuffer texture
    /// </summary>
    internal class Texture
    {
        public string Name { get; set; }
        public bool HasAlpha { get; set; }

        /*
         * name = trackedTexture.Name,
                            level = 1,
                            hasAlpha = hasTransparent,
                            getAlphaFromRGB = false,
                            coordinatesMode = 0,
                            uOffset = 0,
                            vOffset = 0,
                            uScale = 1,
                            vScale = 1,
                            uAng = 0,
                            vAng = 0,
                            wAng = 0,
                            wrapU = true,
                            wrapV = true,
                            coordinatesIndex = 0
                            */
    }
}
