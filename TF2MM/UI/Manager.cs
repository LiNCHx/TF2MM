﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;
using TF2MM.Core;

namespace TF2MM
{
    public partial class Manager : MaterialForm
    {
        private ModHelper modHelper = new ModHelper();
        private DownloadHelper downloadHelper = new DownloadHelper();

        public string tfDirectory = "";

        public Manager()
        {
            InitializeComponent();

            CheckConfig();
        }

        private void CheckConfig()
        {
            if (!LoadConfig())
            {
                Configurator config = new Configurator();
                config.ShowDialog();
                LoadConfig();
            }
        }

        private bool LoadConfig()
        {
            string dir = Properties.Settings.Default.tfPath;
            if (!String.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                tfDirectory = dir;
            }
            else
            {
                return false;
            }

            return true;
        }

        public void ReloadModlist()
        {
            modList.Items.Clear();
            List<ModFile> mods = Utils.GetModList(tfDirectory);
            foreach (ModFile mod in mods)
            {
                MaterialCheckbox checkbox = new MaterialCheckbox
                {
                    Text = mod.Name,
                    Checked = mod.Active,
                    Tag = mod,
                    ContextMenuStrip = modContextMenu,
                };
                checkbox.CheckStateChanged += Checkbox_CheckStateChanged;

                modList.Items.Add(checkbox);
            }
        }

        private void Checkbox_CheckStateChanged(object sender, EventArgs e)
        {
            MaterialCheckbox checkbox = sender as MaterialCheckbox;
            if (checkbox == null) { return; }

            ModFile mod = checkbox.Tag as ModFile;
            if (mod == null) { return; }

            try
            {
                modHelper.SetActive(mod, checkbox.Checked);
            } catch (Exception ex)
            {
                MessageBox.Show("The mod couldn't be " + ((checkbox.Checked) ? "enabled" : "disabled") + ". Please make sure that the game is closed!\n" + ex.Message);
            }
        }

        private void btnConfig_Click(object sender, EventArgs e)
        {
            Configurator config = new Configurator(tfDirectory);
            config.ShowDialog();
            LoadConfig();
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            ReloadModlist();
        }

        private void Manager_Load(object sender, EventArgs e)
        {
            if (FileSystem.IsGameDir(tfDirectory))
            {
                ReloadModlist();
            }
        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            InstallMod();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem contextItem = sender as ToolStripMenuItem;
            if (contextItem == null) { return; }
            MaterialContextMenuStrip contextMenu = contextItem.Owner as MaterialContextMenuStrip;
            if (contextMenu == null) { return; }
            MaterialCheckbox parentCheckbox = contextMenu.SourceControl as MaterialCheckbox;
            if (parentCheckbox == null) { return; }
            ModFile mod = parentCheckbox.Tag as ModFile;
            if (mod == null) { return; }

            DeleteMod(mod);
        }

        private void InstallMod()
        {
            if (modInstallDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if (modHelper.IsInstalled(tfDirectory, modInstallDialog.FileName))
                    {
                        if (MessageBox.Show("This mod already exists! Do you want to update it?", "Update Mod", MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
                        {
                            modHelper.UpdateMod(tfDirectory, modInstallDialog.FileName);
                        }
                        else
                        {
                            modHelper.InstallMod(tfDirectory, modInstallDialog.FileName);
                        }
                    }
                    else
                    {
                        modHelper.InstallMod(tfDirectory, modInstallDialog.FileName);
                    }

                    ReloadModlist();
                    SetStatus("Mod installed succesfully");
                } catch (Exception ex)
                {
                    MessageBox.Show("The mod couldn't be installed!\n" + ex.Message);
                    return;
                }
            }
        }

        private void DeleteMod(ModFile mod)
        {
            DialogResult result = MessageBox.Show("Do you want to create a backup before deleting '" + mod.Name + "'?", "Delete Mod", MessageBoxButtons.YesNoCancel);
            try
            {
                if (result == DialogResult.Yes)
                {
                    modHelper.BackupMod(mod);
                    modHelper.DeleteMod(mod);
                }
                else if (result == DialogResult.No)
                {
                    modHelper.DeleteMod(mod);
                }

                ReloadModlist();
                SetStatus("Mod " + mod.Name + " deleted");
            }
            catch (Exception)
            {
                MessageBox.Show("The mod couln't be deleted. Please close the game first!");
            }
        }

        private void RenameMod(ModFile mod)
        {
            InputDialog dialog = new InputDialog("Enter the new Name for '" + mod.Name + "':", "Rename Mod");
            dialog.ShowDialog();
            if (dialog.DialogResult == DialogResult.OK)
            {
                string fileName = dialog.InputText;
                File.Move(mod.Path, Path.GetDirectoryName(mod.Path) + @"\" + fileName + ((mod.Active) ? ".vpk" : ".vpk.disabled"));
                ReloadModlist();
            }
        }

        public void SetStatus(string status)
        {
            lblStatus.Text = "Status: " + status;
        }

        private void installToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InstallMod();
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReloadModlist();
        }

        private async void btnDownload_Click(object sender, EventArgs e)
        {
            InputDialog dialog = new InputDialog("Enter the direct URL to the mod:", "Download URL");
            dialog.ShowDialog();
            if (dialog.DialogResult == DialogResult.OK)
            {
                try
                {
                    string url = dialog.InputText;
                    await downloadHelper.DownloadFile(tfDirectory, url);
                    await Task.Delay(5000);
                } catch (Exception ex)
                {
                    MessageBox.Show("The mod couldn't be downloaded:\n" + ex.Message, "Download failed");
                }

                ReloadModlist();
            }
        }

        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            Process.Start(FileSystem.GetCustomDir(tfDirectory));
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem contextItem = sender as ToolStripMenuItem;
            if (contextItem == null) { return; }
            MaterialContextMenuStrip contextMenu = contextItem.Owner as MaterialContextMenuStrip;
            if (contextMenu == null) { return; }
            MaterialCheckbox parentCheckbox = contextMenu.SourceControl as MaterialCheckbox;
            if (parentCheckbox == null) { return; }
            ModFile mod = parentCheckbox.Tag as ModFile;
            if (mod == null) { return; }

            RenameMod(mod);
        }

        private void infoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem contextItem = sender as ToolStripMenuItem;
            if (contextItem == null) { return; }
            MaterialContextMenuStrip contextMenu = contextItem.Owner as MaterialContextMenuStrip;
            if (contextMenu == null) { return; }
            MaterialCheckbox parentCheckbox = contextMenu.SourceControl as MaterialCheckbox;
            if (parentCheckbox == null) { return; }
            ModFile mod = parentCheckbox.Tag as ModFile;
            if (mod == null) { return; }

            MessageBox.Show("Information about this mod:\n\n" +
                "Name: " + mod.Name + "\n" +
                "Activate: " + mod.Active.ToString() + "\n" +
                "Path: " + mod.Path,
                "Mod Info");
        }
    }
}
