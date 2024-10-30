﻿using dataPARC.Authorization.CertificateValidation;
using dataPARC.Store.EnterpriseCore.Entities;
using dataPARC.Store.EnterpriseCore.History.Inputs;
using dataPARC.Store.SDK;
using HistorianSdkUtilities.Forms;
using HistorianSdkUtilities.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace HistorianSdkUtilities.Model
{
    internal class MainViewModel : INotifyPropertyChanged
    {
        private string _hostName;
        public string HostName 
        {
            get { return _hostName; }
            set { _hostName = value; OnPropertyChanged(); Save_Settings(); }
        }
        
        private int _port;
        public int Port
        {
            get { return _port; }
            set { _port = value; OnPropertyChanged(); Save_Settings(); }
        }

        public MainViewModel() 
        {
            _hostName = "LocalHost";
            _port = 12340;            

            InterfaceGroups = [];
            InterfaceConfigs = [];
            TagConfigs = [];

            IsTestConnectPending = false;
            IsTagFetchPending = false;

            Load_Settings();
        } 
        
        private void Load_Settings()
        {
            _hostName = Settings.Default.HostName;
            _port = Settings.Default.HistorianPort;
        }
        private void Save_Settings()
        {
            try
            {
                Settings.Default.HostName = _hostName;
                Settings.Default.HistorianPort = _port;
                Settings.Default.Save();
            }
            catch(Exception ex)
            {

            }            
        }

        public void LaunchTagWindow()
        {
            if (SelectedTagConfig != null)
            {
                TagUpdateViewModel tagUpdateViewModel = new TagUpdateViewModel(SelectedTagConfig, HostName, Port);
                TagUpdateWindow win = new TagUpdateWindow(tagUpdateViewModel);
                win.Show();                
            }
            else
            {
                MessageBox.Show("No tag currently selected.");
            }
        }

        public ObservableCollection<InterfaceGroupConfig> InterfaceGroups { get; set; }
        public ObservableCollection<InterfaceConfig> InterfaceConfigs { get; set; }
        public ObservableCollection<TagConfig> TagConfigs { get; set; }
        private TagConfig? _tagConfig { get; set; }
        public TagConfig? SelectedTagConfig
        {
            get
            {
                return _tagConfig;
            }
            set
            {
                _tagConfig = value;
                //LaunchUpdateTagWindowCommand.RaiseCanExecuteChanged();
                OnPropertyChanged();
            }
        }
        private InterfaceGroupConfig? _selectedInterfaceGroupConfig;
        public InterfaceGroupConfig? SelectedInterfaceGroupConfig 
        {
            get
            {
                return _selectedInterfaceGroupConfig;
            }
            set
            {
                _selectedInterfaceGroupConfig = value; OnPropertyChanged();
            } 
        }
        private InterfaceConfig? _selectedInterfaceConfig;
        public InterfaceConfig? SelectedInterfaceConfig 
        {
            get
            {
                return _selectedInterfaceConfig;
            }
            set
            {
                _selectedInterfaceConfig = value; OnPropertyChanged();
            }
        }        

        public async Task TestConnectionToHistorianAndGetInterfacesAsync()
        {
            try
            {
                IsTestConnectPending = true;

                InterfaceGroups.Clear();
                InterfaceConfigs.Clear();
                TagConfigs.Clear();

                bool connected = true;

                await using var client = new ConfigurationClient(HostName, Port, CertificateValidation.AcceptAllCertificates);                                

                SelectedInterfaceGroupConfig = null;
                SelectedInterfaceConfig = null;

                var groupsRes = await client.GetInterfaceGroupsAsync();

                if (groupsRes != null && groupsRes.Value != null)
                {
                    foreach (var group in groupsRes.Value)
                    {
                        InterfaceGroups.Add(group);

                        if (SelectedInterfaceGroupConfig == null && group.Name.ToUpperInvariant() != "SYSTEM_PEM")
                        {
                            SelectedInterfaceGroupConfig = group;
                        }
                    }

                    var interfacesRes = await client.GetInterfacesAsync();

                    if (interfacesRes != null && interfacesRes.Value != null && interfacesRes.Value.Count > 0)
                    {
                        foreach (var intfc in interfacesRes.Value)
                        {
                            InterfaceConfigs.Add(intfc);

                            if (SelectedInterfaceConfig == null && SelectedInterfaceGroupConfig != null && intfc.GroupId == SelectedInterfaceGroupConfig.Id)
                            {
                                SelectedInterfaceConfig = intfc;
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Failed to return interfaces, connection to server may have failed...");
                        connected = false;
                    }
                }
                else
                {
                    MessageBox.Show("Failed to return interface groups, check server connection information and ensure server is reachable...");
                    connected = false;
                }                               

                IsConnected = connected;                
            }
            catch(Exception ex)
            {
                IsConnected = false;
                MessageBox.Show(ex.Message);
            }
            finally
            {
                IsTestConnectPending = false;
            }
        }

        public async Task FetchTagsAsync()
        {
            try
            {
                IsTagFetchPending = true;

                TagConfigs.Clear();

                if (SelectedInterfaceConfig == null)
                {
                    MessageBox.Show("Must select an interface first.");
                    return;
                }

                await using var client = new ConfigurationClient(HostName, Port, CertificateValidation.AcceptAllCertificates);
                
                var readParams = new ReadTagListParameters(new InterfaceQueryIdentifier(SelectedInterfaceConfig.Id));
                    
                var tagReadRes = await client.GetTagsAsync(readParams);

                if (tagReadRes != null && tagReadRes.Status != dataPARC.Common.Validation.Results.GetResultStatus.HasException)
                {                    
                    foreach (var tag in tagReadRes.Value)
                    {                            
                        TagConfigs.Add(tag);
                    }
                }
                else
                {
                    if(tagReadRes != null && tagReadRes.Status == dataPARC.Common.Validation.Results.GetResultStatus.HasException)
                    {
                        MessageBox.Show(tagReadRes.ExceptionMessage);
                    }
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                IsTagFetchPending = false;
            }
        }

        public bool IsButtonsEnabled 
        { 
            get
            {
                return !IsTagFetchPending && !IsTestConnectPending;
            } 
        }

        private bool _isTestConnectPending;
        public bool IsTestConnectPending 
        {
            get { return _isTestConnectPending; }
            set
            {
                if (_isTestConnectPending != value)
                {
                    _isTestConnectPending = value;
                    OnPropertyChanged();
                    OnPropertyChanged("IsButtonsEnabled");
                }
            }
        }

        private bool _isTagFetchPending;
        public bool IsTagFetchPending 
        {
            get { return _isTagFetchPending; }
            set
            {
                if (_isTagFetchPending != value)
                {
                    _isTagFetchPending = value;
                    OnPropertyChanged();
                    OnPropertyChanged("IsButtonsEnabled");
                }
            }
        }        

        public bool? _isConnected;
        public bool? IsConnected
        {
            get { return _isConnected; }
            set 
            { 
                _isConnected = value; 
                OnPropertyChanged();
                OnPropertyChanged("ConnectionFill");
            }
        }

        public Brush ConnectionFill 
        {
            get
            {
                if(IsConnected != null)
                {
                    if (IsConnected.Value)
                    {
                        return new SolidColorBrush(Colors.ForestGreen);
                    }
                    else
                    {
                        return new SolidColorBrush(Colors.Firebrick);
                    }
                }
                else
                {
                    return new SolidColorBrush(Colors.Transparent);
                }
            } 
             
        }        

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
