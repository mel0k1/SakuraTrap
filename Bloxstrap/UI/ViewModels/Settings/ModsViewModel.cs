using System.Windows;
using System.Windows.Input;

using Microsoft.Win32;

using Windows.Win32;
using Windows.Win32.UI.Shell;
using Windows.Win32.Foundation;

using CommunityToolkit.Mvvm.Input;

using Bloxstrap.Models.SettingTasks;
using Bloxstrap.AppData;
using Bloxstrap.UI.Elements.Dialogs;

using System.Collections.ObjectModel;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class ModsViewModel : NotifyPropertyChangedViewModel
    {
        private void OpenModsFolder() => Process.Start("explorer.exe", Paths.Modifications);

        private readonly Dictionary<string, byte[]> FontHeaders = new()
        {
            { "ttf", new byte[4] { 0x00, 0x01, 0x00, 0x00 } },
            { "otf", new byte[4] { 0x4F, 0x54, 0x54, 0x4F } },
            { "ttc", new byte[4] { 0x74, 0x74, 0x63, 0x66 } } 
        };

        private void ManageCustomFont()
        {
            if (!String.IsNullOrEmpty(TextFontTask.NewState))
            {
                TextFontTask.NewState = "";
            }
            else
            {
                var dialog = new OpenFileDialog
                {
                    Filter = $"{Strings.Menu_FontFiles}|*.ttf;*.otf;*.ttc"
                };

                if (dialog.ShowDialog() != true)
                    return;

                string type = dialog.FileName.Substring(dialog.FileName.Length-3, 3).ToLowerInvariant();

                if (!FontHeaders.ContainsKey(type) 
                    || !FontHeaders.Any(x => File.ReadAllBytes(dialog.FileName).Take(4).SequenceEqual(x.Value)))
                {
                    Frontend.ShowMessageBox(Strings.Menu_Mods_Misc_CustomFont_Invalid, MessageBoxImage.Error);
                    return;
                }

                TextFontTask.NewState = dialog.FileName;
            }

            OnPropertyChanged(nameof(ChooseCustomFontVisibility));
            OnPropertyChanged(nameof(DeleteCustomFontVisibility));
        }

        public ICommand OpenModsFolderCommand => new RelayCommand(OpenModsFolder);

        public Visibility ChooseCustomFontVisibility => !String.IsNullOrEmpty(TextFontTask.NewState) ? Visibility.Collapsed : Visibility.Visible;

        public Visibility DeleteCustomFontVisibility => !String.IsNullOrEmpty(TextFontTask.NewState) ? Visibility.Visible : Visibility.Collapsed;

        public ICommand ManageCustomFontCommand => new RelayCommand(ManageCustomFont);

        public ICommand OpenCompatSettingsCommand => new RelayCommand(OpenCompatSettings);

        #region Mod packs
        public ObservableCollection<PackItem> Packs { get; } = new();

        public Visibility PacksVisibility => Packs.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility PacksEmptyVisibility => Packs.Count > 0 ? Visibility.Collapsed : Visibility.Visible;

        public ICommand CreatePackCommand => new RelayCommand(CreatePack);

        public ICommand DeletePackCommand => new RelayCommand<PackItem>(DeletePack);

        public ICommand RenamePackCommand => new RelayCommand<PackItem>(RenamePack);

        public ICommand AddToPackCommand => new RelayCommand<PackItem>(AddToPack);

        public int LooseModsCount { get; private set; }

        public bool HasLooseMods => LooseModsCount > 0;

        private void CreatePack()
        {
            var dialog = new SaveCustomProfileDialog(Strings.Dialog_SaveModPack_Title);
            dialog.ShowDialog();

            if (dialog.Result != MessageBoxResult.OK || String.IsNullOrWhiteSpace(dialog.ProfileName))
                return;

            string name = string.Concat(dialog.ProfileName.Trim().Split(Path.GetInvalidFileNameChars())).Trim();

            if (String.IsNullOrEmpty(name))
                return;

            if (App.ModPacks.Prop.ContainsKey(name))
            {
                Frontend.ShowMessageBox(String.Format(Strings.Menu_Mods_Packs_Exists, name), MessageBoxImage.Error);
                return;
            }

            try
            {
                int count = App.ModPacks.CreateFromCurrentMods(name);

                if (count == 0)
                {
                    Frontend.ShowMessageBox(Strings.Menu_Mods_Packs_NoMods, MessageBoxImage.Information);
                    return;
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("ModsViewModel::CreatePack", $"Failed to create pack '{name}'");
                App.Logger.WriteException("ModsViewModel::CreatePack", ex);

                Frontend.ShowMessageBox(Strings.Menu_Mods_Packs_OperationFailed, MessageBoxImage.Error);
                return;
            }

            RefreshPacks();
        }

        private void DeletePack(PackItem? item)
        {
            if (item is null)
                return;

            MessageBoxResult result = Frontend.ShowMessageBox(
                String.Format(Strings.Menu_Mods_Packs_DeleteConfirm, item.Name),
                MessageBoxImage.Warning,
                MessageBoxButton.YesNo
            );

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                App.ModPacks.Delete(item.Name);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("ModsViewModel::DeletePack", $"Failed to delete pack '{item.Name}'");
                App.Logger.WriteException("ModsViewModel::DeletePack", ex);

                Frontend.ShowMessageBox(Strings.Menu_Mods_Packs_OperationFailed, MessageBoxImage.Error);
                return;
            }

            RefreshPacks();
        }

        private void RenamePack(PackItem? item)
        {
            if (item is null)
                return;

            var dialog = new SaveCustomProfileDialog(Strings.Dialog_SaveModPack_RenameTitle, item.Name);
            dialog.ShowDialog();

            if (dialog.Result != MessageBoxResult.OK || String.IsNullOrWhiteSpace(dialog.ProfileName))
                return;

            string name = string.Concat(dialog.ProfileName.Trim().Split(Path.GetInvalidFileNameChars())).Trim();

            if (String.IsNullOrEmpty(name) || name == item.Name)
                return;

            if (App.ModPacks.Prop.ContainsKey(name))
            {
                Frontend.ShowMessageBox(String.Format(Strings.Menu_Mods_Packs_Exists, name), MessageBoxImage.Error);
                return;
            }

            try
            {
                App.ModPacks.Rename(item.Name, name);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("ModsViewModel::RenamePack", $"Failed to rename pack '{item.Name}'");
                App.Logger.WriteException("ModsViewModel::RenamePack", ex);

                Frontend.ShowMessageBox(Strings.Menu_Mods_Packs_OperationFailed, MessageBoxImage.Error);
                return;
            }

            RefreshPacks();
        }

        private void AddToPack(PackItem? item)
        {
            if (item is null)
                return;

            int count;

            try
            {
                count = App.ModPacks.AddCurrentMods(item.Name);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("ModsViewModel::AddToPack", $"Failed to add mods to pack '{item.Name}'");
                App.Logger.WriteException("ModsViewModel::AddToPack", ex);

                Frontend.ShowMessageBox(Strings.Menu_Mods_Packs_OperationFailed, MessageBoxImage.Error);
                return;
            }

            if (count > 0)
                Frontend.ShowMessageBox(String.Format(Strings.Menu_Mods_Packs_AddedToPack, count, item.Name), MessageBoxImage.Information);
            else
                Frontend.ShowMessageBox(Strings.Menu_Mods_Packs_NoMods, MessageBoxImage.Information);

            RefreshPacks();
        }

        private void RefreshPacks()
        {
            Packs.Clear();

            LooseModsCount = App.ModPacks.CountLooseMods();
            OnPropertyChanged(nameof(HasLooseMods));

            foreach (string name in App.ModPacks.Names)
                Packs.Add(new PackItem(name));

            OnPropertyChanged(nameof(PacksVisibility));
            OnPropertyChanged(nameof(PacksEmptyVisibility));
        }
        #endregion

        #region Client integrity
        public string IntegrityReport { get; set; } = "";

        public Visibility IntegrityReportVisibility => String.IsNullOrEmpty(IntegrityReport) ? Visibility.Collapsed : Visibility.Visible;

        public ICommand CheckIntegrityCommand => new RelayCommand(CheckIntegrity);

        public ICommand RestoreOriginalsCommand => new RelayCommand(RestoreOriginals);

        private void CheckIntegrity()
        {
            var files = ClientIntegrity.Check();

            if (files.Count == 0)
            {
                IntegrityReport = Strings.Menu_Mods_Integrity_NoMods;
            }
            else
            {
                var builder = new StringBuilder();

                foreach (var file in files)
                {
                    string status = file.Status switch
                    {
                        ModFileStatus.Applied => Strings.Menu_Mods_Integrity_StatusApplied,
                        ModFileStatus.Modified => Strings.Menu_Mods_Integrity_StatusModified,
                        ModFileStatus.SourceMissing => Strings.Menu_Mods_Integrity_StatusSourceMissing,
                        ModFileStatus.ClientMissing => Strings.Menu_Mods_Integrity_StatusClientMissing,
                        _ => Strings.Common_Unknown
                    };

                    builder.AppendLine($"{file.RelativePath} — {status}");
                }

                IntegrityReport = builder.ToString().TrimEnd();
            }

            OnPropertyChanged(nameof(IntegrityReport));
            OnPropertyChanged(nameof(IntegrityReportVisibility));
        }

        private void RestoreOriginals()
        {
            MessageBoxResult result = Frontend.ShowMessageBox(
                Strings.Menu_Mods_Integrity_RestoreConfirm,
                MessageBoxImage.Warning,
                MessageBoxButton.YesNo
            );

            if (result != MessageBoxResult.Yes)
                return;

            var (removed, failed) = ClientIntegrity.RestoreOriginals();

            IntegrityReport = String.Format(Strings.Menu_Mods_Integrity_Restored, removed, failed);

            OnPropertyChanged(nameof(IntegrityReport));
            OnPropertyChanged(nameof(IntegrityReportVisibility));

            RefreshPacks();
        }
        #endregion

        public ModPresetTask OldAvatarBackgroundTask { get; } = new("OldAvatarBackground", @"ExtraContent\places\Mobile.rbxl", "OldAvatarBackground.rbxl");

        public ModPresetTask OldCharacterSoundsTask { get; } = new("OldCharacterSounds", new()
        {
            { @"content\sounds\action_footsteps_plastic.mp3", "Sounds.OldWalk.mp3"  },
            { @"content\sounds\action_jump.mp3",              "Sounds.OldJump.mp3"  },
            { @"content\sounds\action_get_up.mp3",            "Sounds.OldGetUp.mp3" },
            { @"content\sounds\action_falling.mp3",           "Sounds.Empty.mp3"    },
            { @"content\sounds\action_jump_land.mp3",         "Sounds.Empty.mp3"    },
            { @"content\sounds\action_swim.mp3",              "Sounds.Empty.mp3"    },
            { @"content\sounds\impact_water.mp3",             "Sounds.Empty.mp3"    }
        });

        public EmojiModPresetTask EmojiFontTask { get; } = new();

        public EnumModPresetTask<Enums.CursorType> CursorTypeTask { get; } = new("CursorType", new()
        {
            {
                Enums.CursorType.From2006, new()
                {
                    { @"content\textures\Cursors\KeyboardMouse\ArrowCursor.png",    "Cursor.From2006.ArrowCursor.png"    },
                    { @"content\textures\Cursors\KeyboardMouse\ArrowFarCursor.png", "Cursor.From2006.ArrowFarCursor.png" }
                }
            },
            {
                Enums.CursorType.From2013, new()
                {
                    { @"content\textures\Cursors\KeyboardMouse\ArrowCursor.png",    "Cursor.From2013.ArrowCursor.png"    },
                    { @"content\textures\Cursors\KeyboardMouse\ArrowFarCursor.png", "Cursor.From2013.ArrowFarCursor.png" }
                }
            }
        });

        public FontModPresetTask TextFontTask { get; } = new();

        private void OpenCompatSettings()
        {
            string path = new RobloxPlayerData().ExecutablePath;

            if (File.Exists(path))
                PInvoke.SHObjectProperties(HWND.Null, SHOP_TYPE.SHOP_FILEPATH, path, "Compatibility");
            else
                Frontend.ShowMessageBox(Strings.Common_RobloxNotInstalled, MessageBoxImage.Error);

        }
    }

    public class PackItem
    {
        private readonly ModPack _pack;

        public PackItem(string name)
        {
            Name = name;
            _pack = App.ModPacks.Prop[name];
        }

        public string Name { get; }

        public string FilesLabel => String.Format(Strings.Menu_Mods_Packs_FilesCount, _pack.Files.Count);

        public IReadOnlyList<string> Files => _pack.Files;

        public bool IsEnabled
        {
            get => _pack.Enabled;
            set => App.ModPacks.SetEnabled(Name, value);
        }
    }
}
