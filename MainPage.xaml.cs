using Microsoft.Maui.Controls;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;


namespace BLE;

public partial class MainPage : ContentPage
{
    private IDevice device;
    private IBluetoothLE _ble;
    private IAdapter _adapter;
    private IService service;
    private ICharacteristic characteristic;
    private ObservableCollection<string[]> list;

    Button scanButton;
    Label scanLabel;


    [Obsolete]
    public MainPage()
    {
        InitializeComponent();

        _ble = CrossBluetoothLE.Current;
        _adapter = CrossBluetoothLE.Current.Adapter;


        scanLabel =  new Label();
        // Create a new Button control to trigger the BLE device scanning
        scanButton = new Button();
        scanButton.Text = "Scan for Devices";
        scanButton.Clicked += ScanForDevices_Clicked;

        scanLabel.HeightRequest = 60;
        scanLabel.FontSize = 20;
        scanLabel.VerticalTextAlignment = TextAlignment.Center;
        scanLabel.HorizontalTextAlignment = TextAlignment.Center;
        // Create a new ListView control to display the discovered BLE devices

        ListView listView = new ListView();
        listView.RowHeight = 60;


        listView.ItemTemplate = new DataTemplate(() =>
        {
            Label nameLabel = new Label();
            nameLabel.SetBinding(Label.TextProperty, new Binding("[0]"));

            Label macAddressLabel = new Label();
            macAddressLabel.SetBinding(Label.TextProperty, new Binding("[1]"));

            Label RssiLabel = new Label();
            RssiLabel.SetBinding(Label.TextProperty, new Binding("[2]"));

            StackLayout stackLayout = new StackLayout();
            stackLayout.Children.Add(nameLabel);
            stackLayout.Children.Add(macAddressLabel);
            stackLayout.Children.Add(RssiLabel);
     



            ViewCell viewCell = new ViewCell();
 
            viewCell.View = stackLayout;
        

            return viewCell;
        });

        list = new ObservableCollection<string[]>();
        listView.ItemsSource = list;

        // Create a new StackLayout to hold the Button and ListView
        StackLayout stackLayout = new StackLayout();
        stackLayout.Children.Add(scanButton);
        stackLayout.Children.Add(scanLabel);
        stackLayout.Children.Add(listView);
        stackLayout.Margin = new Thickness(20);

        // Set the Content of the ContentPage to the StackLayout
        Content =  stackLayout;
        _adapter.ScanTimeout = 30000;

        _adapter.DeviceConnected += _adapter_DeviceConnectedAsync;
        // Subscribe to the DeviceDiscovered event to add discovered devices to the ListView
        _adapter.DeviceDiscovered += async (sender, args) =>
        {
            // Add the discovered device to the ObservableCollection
            //_devices.Add(args.Device);
            string[] str = new string[3];
            if(args.Device.Name != null)
                str[0] = args.Device.Name; 
            else
                str[0] = "Unknown";
        
            str[1]= args.Device.Id.ToString();
            str[2] = args.Device.Rssi.ToString();

            list.Add(str);

            scanButton.Text = list.Count.ToString();

            if(args.Device.Name == "BLE-Secure-Server")
            {
                device = args.Device;
                //await _adapter.ConnectToKnownDeviceAsync(Guid.Parse("47:D4:18:D8:F6:C3"));
                await _adapter.ConnectToDeviceAsync(device);
            }

        };

    }

    [Obsolete]
    private async void _adapter_DeviceConnectedAsync(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
    {
        await toastPrintAsync("connected.");
        await GetDataFromDeviceAsync();

        var services = await device.GetServicesAsync();
        var name = services[2].Name;
        var id = services[2].Id;

        var characteristics = await service.GetCharacteristicsAsync();


    }

    [Obsolete]
    private async void ScanForDevices_Clicked(object sender, EventArgs e)
    {
        // Clear the existing list of devices
        list.Clear();
        // Start scanning for devices
        await _adapter.StartScanningForDevicesAsync();

        if(!_adapter.IsScanning)
        {
            await toastPrintAsync("Scanning is done.");
        }

    }

    [Obsolete]
    private async Task GetDataFromDeviceAsync()
    {
        try
        {
            await _adapter.ConnectToDeviceAsync(device);


            service = await device.GetServiceAsync(Guid.Parse("0000180F-0000-1000-8000-00805F9B34FB"));
            characteristic = await service.GetCharacteristicAsync(Guid.Parse("00002A19-0000-1000-8000-00805F9B34FB"));

            characteristic.ValueUpdated += async (sender, args) =>
            {
                var data = args.Characteristic.Value;
                var dataString = System.Text.Encoding.UTF8.GetString(data);
                //await toastPrintAsync(dataString);
                await Device.InvokeOnMainThreadAsync(() =>
                {
                    scanLabel.Text = dataString;
                });
                await sendDataAsync("hello");

            };

            await characteristic.StartUpdatesAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    [Obsolete]
    private async Task toastPrintAsync(string message)
    {
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        string text = message;
        ToastDuration duration = ToastDuration.Long;
        double fontSize = 18;

        var toast = Toast.Make(text, duration, fontSize);

        await Device.InvokeOnMainThreadAsync(() =>
        {
             toast.Show(cancellationTokenSource.Token);
        });
    }
    async Task sendDataAsync(string data)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(data);
        await characteristic.WriteAsync(bytes);
    }


}

