using OpenTK.Graphics.OpenGL;
using OpenTK;
using WindEditor;
using System.IO;
using GameFormatReader.Common;
using System.ComponentModel;
using JStudio.J3D;
using JStudio.OpenGL;
using J3DModelViewer.Framework;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows;
using System.Collections.Generic;
using Microsoft.WindowsAPICodePack.Dialogs;
using JStudio.Framework;

namespace J3DModelViewer.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public bool HasLoadedModel { get { return m_loadedModels.Count > 0; } }
        public J3D MainModel { get { return m_loadedModels.Count > 0 ? m_loadedModels[0] : null; } }
        public SceneGraphViewModel MainScenegraph { get { return m_sceneGraphs.Count > 0 ? m_sceneGraphs[0] : null; } }
        public ModelRenderOptionsViewModel ViewOptions { get { return m_modelRenderOptions; } }
        public HighresScreenshotViewModel HighResScreenshot { get { return m_highresScreenshot; } }

        #region Commands
        public ICommand OpenModelCommand { get { return new RelayCommand(x => OnUserRequestLoadAsset(true)); } }
        public ICommand OpenModelAdditiveCommand { get { return new RelayCommand(x => OnUserRequestLoadAsset(false)); } }
        public ICommand LoadAnimationCommand { get { return new RelayCommand(x => OnUserRequestLoadAsset(false)); } }
        public ICommand CloseModelCommand { get { return new RelayCommand(x => OnUserRequestCloseModel()); } }
        public ICommand ExitApplicationCommand { get { return new RelayCommand(x => OnUserRequestApplicationExit()); } }
        public ICommand ExportMeshesToObjCommand { get { return new RelayCommand(x => OnUserRequestExportMeshes()); } }
        public ICommand DeselectBoneAnimationCommand { get { return new RelayCommand(x => { if (MainModel != null) MainModel.SetBoneAnimation(null); }, x => MainModel != null && MainModel.CurrentBoneAnimation != null); } }
        public ICommand DeselectMaterialAnimationCommand { get { return new RelayCommand(x => { if (MainModel != null) MainModel.SetMaterialAnimation(null); }, x => MainModel != null && MainModel.CurrentMaterialAnimation != null); } }
        public ICommand DeselectExternalMaterialCommand { get { return new RelayCommand(x => { if (MainModel != null) MainModel.SetExternalMaterial(null); }, x => MainModel != null && MainModel.CurrentExternalMaterial != null); } }
        #endregion

        // Rendering
        private WCamera m_renderCamera;
        private int m_viewportHeight;
        private int m_viewportWidth;
        private float m_timeSinceStartup;
        private GLControl m_glControl;
        private Vector3 m_glControlClearColor;
        private System.Diagnostics.Stopwatch m_dtStopwatch;

        private ScreenspaceQuad m_screenQuad;
        private Shader m_alphaVisualizationShader;
        private WFrameBuffer m_frameBuffer;

        private List<J3D> m_loadedModels;
        private List<SceneGraphViewModel> m_sceneGraphs;

        GXLight m_mainLight;
        GXLight m_secondaryLight;

        private HighresScreenshotViewModel m_highresScreenshot;
        private ModelRenderOptionsViewModel m_modelRenderOptions;
        private WLineBatcher m_lineBatcher;

        public MainWindowViewModel()
        {
            // Override the Current Directory with one we calculate ourself. This solves the problem of assigning the application as the
            // default application for a filetype and it having its CurrentDirectory be System32.
            Environment.CurrentDirectory = ApplicationExtensions.GetBasePath();

            m_highresScreenshot = new HighresScreenshotViewModel();
            m_modelRenderOptions = new ModelRenderOptionsViewModel();

            m_renderCamera = new WCamera();
            m_loadedModels = new List<J3D>();
            m_sceneGraphs = new List<SceneGraphViewModel>();

            m_renderCamera.Transform.Position = new Vector3(500, 75, 500);
            m_renderCamera.Transform.Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, WMath.DegreesToRadians(45f));
            m_dtStopwatch = new System.Diagnostics.Stopwatch();
            Application.Current.MainWindow.Closing += OnMainWindowClosing;

            Random rnd = new Random((int)DateTime.Now.ToBinary());
            m_glControlClearColor = ColorUtils.HSVtoRGB(new Vector3(rnd.Next(255) / 255f, 0.7f, 0.85f));
        }

        internal void OnMainEditorWindowLoaded(GLControl child)
        {
            m_glControl = child;
            m_frameBuffer = new WFrameBuffer(m_glControl.Width, m_glControl.Height);
            m_lineBatcher = new WLineBatcher();

            m_alphaVisualizationShader = new Shader("AlphaVisualize");
            m_alphaVisualizationShader.CompileSource(File.ReadAllText("resources/shaders/Debug_AlphaVisualizer.vert"), ShaderType.VertexShader);
            m_alphaVisualizationShader.CompileSource(File.ReadAllText("resources/shaders/Debug_AlphaVisualizer.frag"), ShaderType.FragmentShader);
            m_alphaVisualizationShader.LinkShader();

            m_screenQuad = new ScreenspaceQuad();

            // Set up the Editor Tick Loop
            System.Windows.Forms.Timer editorTickTimer = new System.Windows.Forms.Timer();
            editorTickTimer.Interval = 16; //ms
            editorTickTimer.Tick += (o, args) =>
            {
                DoApplicationTick();
            };
            editorTickTimer.Enabled = true;

            var lightPos = new Vector4(250, 200, 250, 0);
            m_mainLight = new GXLight(lightPos, -lightPos.Normalized(), new Vector4(1, 0, 1, 1), new Vector4(1.075f, 0, 0, 0), new Vector4(1.075f, 0, 0, 0));
            m_secondaryLight = new GXLight(lightPos, -lightPos.Normalized(), new Vector4(0, 0, 1, 1), new Vector4(1.075f, 0, 0, 0), new Vector4(1.075f, 0, 0, 0));
            m_secondaryLight.Position = new Vector4(CalculateLightPosition((float)Math.PI / 2f), 0);

            // Check to see if there's any file on the command line argument now that we've initialized, incase they opened via double clicking on a file.
            string[] cmdArgs = Environment.GetCommandLineArgs();
            for (int i = 1; i < cmdArgs.Length; i++)
            {
                LoadAssetFromFilepath(cmdArgs[i], false);
            }
        }

        public void OnFilesDropped(string[] droppedFilePaths)
        {
            // Only check the first one for now...
            if (droppedFilePaths.Length > 0)
                LoadAssetFromFilepath(droppedFilePaths[0], true);
        }

        private void OnUserRequestLoadAsset(bool unloadExisting)
        {
            var ofd = new CommonOpenFileDialog();
            ofd.Title = "Choose File...";
            ofd.IsFolderPicker = false;
            ofd.AddToMostRecentlyUsedList = false;
            ofd.AllowNonFileSystemItems = false;
            ofd.EnsureFileExists = true;
            ofd.EnsurePathExists = true;
            ofd.EnsureReadOnly = false;
            ofd.EnsureValidNames = true;
            ofd.Multiselect = false;
            ofd.ShowPlacesList = true;

            // If they haven't loaded any models, they can't load any of the associated animation data.
            string allSupportedExtensions = "*.bmd,*.bdl,*.bck, *.btk, *.bmt";
            string onlyModelExtensions = "*.bmd, *.bdl";

            string extensions = HasLoadedModel ? allSupportedExtensions : onlyModelExtensions;

            ofd.Filters.Add(new CommonFileDialogFilter(string.Format("Supported Files ({0})", extensions), extensions));
            ofd.Filters.Add(new CommonFileDialogFilter("All Files (*.*)", "*.*"));

            if (ofd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                LoadAssetFromFilepath(ofd.FileName, unloadExisting);
            }
        }

        private void OnUserRequestCloseModel()
        {
            foreach (var j3d in m_loadedModels)
                j3d.Dispose();

            m_loadedModels.Clear();
            m_sceneGraphs.Clear();
        }

        private void OnUserRequestApplicationExit()
        {
            // This attempts to close the application, which invokes the normal window close events.
            App.Current.MainWindow.Close();
        }

        private void LoadAssetFromFilepath(string filePath, bool unloadExisting)
        {
            if (!File.Exists(filePath))
                Console.WriteLine("Cannot load: \"{0}\", not a file!", filePath);

            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

            switch (fileExtension)
            {
                case ".bdl":
                case ".bmd":
                    {
                        if (unloadExisting)
                        {
                            foreach (var model in m_loadedModels)
                                model.Dispose();
                            m_loadedModels.Clear();
                            m_sceneGraphs.Clear();
                        }

                        var newModel = new J3D(fileName);
                        using (EndianBinaryReader reader = new EndianBinaryReader(new FileStream(filePath, FileMode.Open, FileAccess.Read), Endian.Big))
                            newModel.LoadFromStream(reader, true, true);

                        newModel.SetHardwareLight(0, m_mainLight);
                        newModel.SetHardwareLight(1, m_secondaryLight);

                        // Apply patches for Wind Waker by default, since they don't seem to break anything else.
                        newModel.SetTextureOverride("ZBtoonEX", "resources/textures/ZBtoonEX.png");
                        newModel.SetTextureOverride("ZAtoon", "resources/textures/ZAtoon.png");
                        newModel.SetColorWriteOverride("eyeLdamA", false);
                        newModel.SetColorWriteOverride("eyeLdamB", false);
                        newModel.SetColorWriteOverride("mayuLdamA", false);
                        newModel.SetColorWriteOverride("mayuLdamB", false);
                        newModel.SetColorWriteOverride("eyeRdamA", false);
                        newModel.SetColorWriteOverride("eyeRdamB", false);
                        newModel.SetColorWriteOverride("mayuRdamA", false);
                        newModel.SetColorWriteOverride("mayuRdamB", false);

                        m_loadedModels.Add(newModel);
                        m_sceneGraphs.Add(new SceneGraphViewModel(newModel, newModel.INF1Tag.HierarchyRoot, ""));
                    }
                    break;

                case ".bck":
                    {
                        if (MainModel != null)
                        {
                            if (unloadExisting)
                                MainModel.UnloadBoneAnimations();
                            MainModel.LoadBoneAnimation(filePath);

                            // Automatically play the latest animation loaded.
                            MainModel.SetBoneAnimation(fileName);
                        }
                    }
                    break;

                case ".btk":
                    {
                        if (MainModel != null)
                        {
                            if (unloadExisting)
                                MainModel.UnloadMaterialAnimations();
                            MainModel.LoadMaterialAnim(filePath);

                            // Automatically play the latest animation loaded.
                            MainModel.SetMaterialAnimation(fileName);
                        }
                    }
                    break;

                case ".bmt":
                    {
                        if (MainModel != null)
                        {
                            if (unloadExisting)
                                MainModel.UnloadExternalMaterials();
                            MainModel.LoadExternalMaterial(filePath);

                            // Automatically set the latest external material loaded.
                            MainModel.SetExternalMaterial(fileName);
                        }

                    }
                    break;
            }

            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("MainModel"));
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("MainScenegraph"));
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("HasLoadedModel"));
            }
        }

        private void DoApplicationTick()
        {
            // Poll the mouse at a high resolution
            System.Drawing.Point mousePos = m_glControl.PointToClient(System.Windows.Forms.Cursor.Position);

            mousePos.X = WMath.Clamp(mousePos.X, 0, m_glControl.Width);
            mousePos.Y = WMath.Clamp(mousePos.Y, 0, m_glControl.Height);
            WInput.SetMousePosition(new Vector2(mousePos.X, mousePos.Y));

            ProcessTick();
            WInput.Internal_UpdateInputState();

            m_glControl.SwapBuffers();
        }

        private void ProcessTick()
        {
            GL.ClearColor(m_glControlClearColor.X, m_glControlClearColor.Y, m_glControlClearColor.Z, 1f);

            // If the user has requested a higher resolution screenshot, we're going to resize the backbuffer if required.
            if (m_highresScreenshot.UserRequestedScreenshot)
            {
                int scaledWidth = (int)(m_viewportWidth * m_highresScreenshot.ResolutionMultiplier);
                int scaledHeight = (int)(m_viewportHeight * m_highresScreenshot.ResolutionMultiplier);

                if (scaledWidth != m_frameBuffer.Width || scaledHeight != m_frameBuffer.Height)
                    m_frameBuffer.ResizeBuffer(scaledWidth, scaledHeight);
            }

            RenderFrame();

            // Now that we've rendered to the framebuffer, if they've requested a screenshot, write that to disk.
            if (m_highresScreenshot.UserRequestedScreenshot)
            {
                CaptureScreenshotToDisk();

                // Resize the buffer back down if required.
                if (m_frameBuffer.Width != m_viewportWidth || m_frameBuffer.Height != m_viewportHeight)
                    m_frameBuffer.ResizeBuffer(m_viewportWidth, m_viewportHeight);

                m_highresScreenshot.UserRequestedScreenshot = false;
            }
        }

        private void RenderFrame()
        {
            m_frameBuffer.Bind();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            GL.Viewport(0, 0, m_frameBuffer.Width, m_frameBuffer.Height);

            float deltaTime = m_dtStopwatch.ElapsedMilliseconds / 1000f;
            m_dtStopwatch.Restart();
            m_renderCamera.Tick(deltaTime);
            m_lineBatcher.Tick(deltaTime);

            deltaTime = WMath.Clamp(deltaTime, 0, 0.25f); // quarter second max because debugging
            m_timeSinceStartup += deltaTime;

            if(m_modelRenderOptions.AnimateLight)
            {
                // Rotate our light
                float angleInRad = m_timeSinceStartup % WMath.DegreesToRadians(360f);
                m_mainLight.Position = new Vector4(CalculateLightPosition(angleInRad), 0);
            }

            if (m_modelRenderOptions.ShowGrid)
            {
                DrawFixedGrid();
            }

            foreach (var j3d in m_loadedModels)
            {
                j3d.SetHardwareLight(0, m_mainLight);
                j3d.Tick(deltaTime);
                j3d.Render(m_renderCamera.ViewMatrix, m_renderCamera.ProjectionMatrix, Matrix4.Identity);
            }

            if (m_modelRenderOptions.ShowPivot)
            {
                m_lineBatcher.DrawLine(Vector3.Zero, new Vector3(50, 0, 0), WLinearColor.Red, 0, 0);
                m_lineBatcher.DrawLine(Vector3.Zero, new Vector3(0, 50, 0), WLinearColor.Green, 0, 0);
                m_lineBatcher.DrawLine(Vector3.Zero, new Vector3(0, 0, 50), WLinearColor.Blue, 0, 0);
            }

            if (m_modelRenderOptions.ShowBoundingBox)
            {
                foreach (var j3d in m_loadedModels)
                    j3d.DrawBoundsForShapes(true, false, m_lineBatcher);
            }

            if (m_modelRenderOptions.ShowBoundingSphere)
            {
                foreach (var j3d in m_loadedModels)
                    j3d.DrawBoundsForShapes(false, true, m_lineBatcher);
            }

            if (m_modelRenderOptions.ShowBoneBoundingBox)
            {
                foreach (var j3d in m_loadedModels)
                    j3d.DrawBoundsForJoints(true, false, m_lineBatcher);
            }

            if (m_modelRenderOptions.ShowBoneBoundingSphere)
            {
                foreach (var j3d in m_loadedModels)
                    j3d.DrawBoundsForJoints(false, true, m_lineBatcher);
            }

            if (m_modelRenderOptions.ShowBones)
            {
                foreach (var j3d in m_loadedModels)
                    j3d.DrawBones(m_lineBatcher);
            }

            // Debug Rendering
            if (WInput.GetKey(Key.I))
            {
                GL.Disable(EnableCap.CullFace);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactorSrc.DstAlpha, BlendingFactorDest.Zero);

                m_alphaVisualizationShader.Bind();
                m_screenQuad.Draw();
            }

            // Blit the framebuffer to the backbuffer so it shows up on screen.
            m_lineBatcher.Render(m_renderCamera.ViewMatrix, m_renderCamera.ProjectionMatrix);
            m_frameBuffer.BlitToBackbuffer(m_viewportWidth, m_viewportHeight);
        }

        private void CaptureScreenshotToDisk()
        {
            byte[] pixelData = m_frameBuffer.ReadPixels(0, 0, m_frameBuffer.Width, m_frameBuffer.Height);

            using (Bitmap bmp = new Bitmap(m_frameBuffer.Width, m_frameBuffer.Height))
            {
                Rectangle rect = new Rectangle(0, 0, m_frameBuffer.Width, m_frameBuffer.Height);
                System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppRgb);

                //Lock the bitmap for writing, copy the bits and then unlock for saving.
                IntPtr ptr = bmpData.Scan0;
                //byte[] imageData = pixel;
                Marshal.Copy(pixelData, 0, ptr, pixelData.Length);
                bmp.UnlockBits(bmpData);
                bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

                // Bitmaps will throw an exception if the output folder doesn't exist so...
                string outputName = string.Format("Screenshots/{0:yyyy-MM-dd_hh-mm-ss-tt}.png", DateTime.UtcNow);
                Directory.CreateDirectory(Path.GetDirectoryName(outputName));
                bmp.Save(outputName);
            }
        }

        internal void OnViewportResized(int width, int height)
        {
            m_viewportWidth = width;
            m_viewportHeight = height;
            m_renderCamera.AspectRatio = width / (float)height;
            m_frameBuffer.ResizeBuffer(width, height);
        }

        private void OnMainWindowClosing(object sender, CancelEventArgs e)
        {
            if (m_frameBuffer != null)
                m_frameBuffer.Dispose();

            if (m_alphaVisualizationShader != null)
                m_alphaVisualizationShader.Dispose();

            if (m_modelRenderOptions != null)
                m_modelRenderOptions.SaveSettings();

            foreach (var j3d in m_loadedModels)
                j3d.Dispose();

            m_lineBatcher.ReleaseResources();
        }

        private void DrawFixedGrid()
        {
            int numCellsToDraw = 64;

            int rangeInCells = numCellsToDraw / 2;
            int majorLineInterval = numCellsToDraw / 8;

            int numLines = numCellsToDraw + 1;
            int axesIndex = numCellsToDraw / 2;

            float perspectiveGridSize = 400000;

            for (int lineIndex = 0; lineIndex < numLines; lineIndex++)
            {
                bool isMajorLine = ((lineIndex - rangeInCells) % majorLineInterval) == 0;

                Vector3 a = new Vector3(), b = new Vector3();
                a.X = (perspectiveGridSize / 4f) * (-1f + 2f * lineIndex / numCellsToDraw);
                b.X = a.X;

                a.Y = b.Y = 0;

                a.Z = (perspectiveGridSize / 4f);
                b.Z = -(perspectiveGridSize / 4f);

                WLinearColor lineColor;
                float lineThickness = 0;

                if (lineIndex == axesIndex)
                {
                    lineColor = new WLinearColor(70 / 255f, 70 / 255f, 70 / 255f);
                    lineThickness = 0f;
                }
                else if (isMajorLine)
                {
                    lineColor = new WLinearColor(40 / 255f, 40 / 255f, 40 / 255f);
                }
                else
                {
                    lineColor = new WLinearColor(20 / 255f, 20 / 255f, 20 / 255f);
                }

                m_lineBatcher.DrawLine(a, b, lineColor, lineThickness, 0f);

                a.Z = a.X;
                b.Z = b.X;
                a.X = (perspectiveGridSize / 4f);
                b.X = -(perspectiveGridSize / 4f);
                m_lineBatcher.DrawLine(a, b, lineColor, lineThickness, 0f);
            }
        }

        private Vector3 CalculateLightPosition(float angleInRad)
        {
            Quaternion lightRot = Quaternion.FromAxisAngle(Vector3.UnitY, angleInRad);
            Vector3 newLightPos = Vector3.Transform(new Vector3(-500, 0, 0), lightRot) + new Vector3(0, 50, 0);
            return newLightPos;
        }

        private void OnUserRequestExportMeshes()
        {
            var sfd = new CommonSaveFileDialog();
            sfd.Title = "Save File...";
            sfd.AddToMostRecentlyUsedList = false;
            //sfd.EnsureFileExists = true;
            //sfd.EnsurePathExists = true;
            sfd.EnsureReadOnly = false;
            sfd.EnsureValidNames = true;
            sfd.ShowPlacesList = true;
            sfd.Filters.Add(new CommonFileDialogFilter("Obj Files", "*.obj"));
            sfd.Filters.Add(new CommonFileDialogFilter("All Files", "*.*"));

            if (sfd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string directoryName = Path.GetDirectoryName(sfd.FileName);
                Directory.CreateDirectory(directoryName);

                for (int i = 0; i < m_loadedModels.Count; i++)
                {
                    string exportName = Path.GetFileName(sfd.FileName);

                    // If they have more than one model currently loaded, export each obj 
                    if (m_loadedModels.Count > 1)
                        exportName += string.Format("_{0}.obj", i.ToString("00"));

                    string exportPath = Path.Combine(directoryName, exportName);
                    ObjExporter.Export(m_loadedModels[i], exportPath);
                }
            }
        }
    }
}
