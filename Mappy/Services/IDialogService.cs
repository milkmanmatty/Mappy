namespace Mappy.Services
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Windows.Forms;

    using Mappy.Models;
    using Mappy.Views;

    public interface IDialogService
    {
        string AskUserToChooseMap(IList<string> maps);

        string AskUserToOpenFile();

        string AskUserToSaveFile();

        string AskUserToSaveMinimap();

        string AskUserToSaveMapImage();

        Models.MapImageExportOptions AskUserForMapImageExportOptions();

        string AskUserToChooseMinimap();

        string AskUserToSaveHeightmap();

        string AskUserToChooseHeightmap(int width, int height);

        SectionImportExportPaths AskUserToChooseSectionImportPaths();

        SectionImportExportPaths AskUserToChooseSectionExportPaths();

        bool CapturePreferences();

        Size AskUserNewMapSize();

        Size AskUserResizeMapSize(int currentWidth, int currentHeight);

        Color? AskUserGridColor(Color previousColor);

        DialogResult AskUserToDiscardChanges();

        MapAttributesResult AskUserForMapAttributes(MapAttributesResult r);

        int? AskUnitPlayerNumber(IWin32Window owner, int defaultPlayer = 1);

        string AskUserForNewSchemaType(string defaultSchemaType, Func<string, string> validateTrimmedName = null);

        int? PickUnitPlayerAtScreenPoint(Point screenLocation);

        void ShowError(string message);

        IProgressView CreateProgressView();

        void ShowAbout();

        void ShowInfo();

        FlipOptions AskUserForFlipOptions();
    }
}
