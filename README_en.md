[Deutsch](README.md)

# Kaenx
Kaenx is an Open Source Software to configure KNX Devices.  
You will find the current development state in the german [Forum](https://knx-user-forum.de/forum/öffentlicher-bereich/knx-eib-forum/diy-do-it-yourself/1672351-kaenx-open-source-inbetriebnahme-software). 
You can also report bugs and feature requests there.  
  
# Current State
Currently the software is an UWP-App so it only supports Windows 10/11.  
After the recoding with [UNO-Platform](https://platform.uno) (or [.NET Maui](https://docs.microsoft.com/en-us/dotnet/maui/what-is-maui)) it will also support Linux, MacOS, iOS and Android.  
There is no timeshift for the recoding. We will start with this after we can configure all KNX Devices with the software.    
  
# Installing
~~There are two ways to install the App:  
 - ~~Microsoft Store  
    Only major releases
 - ~~App Installer File  
    Also small releases. Updates will be installed automatically.    
    It is mandatory to install the Publisher Certificate as an administrator. The path has to be "Trusted Root Certification Authorities" on the local computer.
    
Kaenx **wont be availible** for direct download due to the complete recode in the future.  
If you are still interested in testing it you can compile it with Visual Studio by your own.  

# Images
[![Kaenx Ansicht Topologie](/Images/Topologie.png)](/Images/)
  
# Privacy
The app collects anonymized data about crashes and defined events.  
To do this we use App-Insights and App-Center (data will be deleted after 90 days).  
For more information about App-Insight click [here](https://docs.microsoft.com/en-us/azure/azure-monitor/app/data-retention-privacy).
