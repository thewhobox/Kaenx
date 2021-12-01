[English](Readme_en.md)

# Kaenx
Kaenx ist eine Open Source Inbetriebnahme Software für KNX Geräte.  
Die aktive Entwicklung, sowie kleiner Bugs können gerne im [Forum](https://knx-user-forum.de/forum/öffentlicher-bereich/knx-eib-forum/diy-do-it-yourself/1672351-kaenx-open-source-inbetriebnahme-software) gemeldet werden.  
  
# Aktueller Stand
Die Anwendung ist eine UWP-App und unterstützt somit nur Windows 10/11.  
Mit der Umstellung auf [UNO-Platform](https://platform.uno) (oder [.NET Maui](https://docs.microsoft.com/de-de/dotnet/maui/what-is-maui)) wird die Anwendung auch Linux, MacOS, iOS und Android unterstützen.  
Die Umstellung erfolgt allerdings erst, nachdem das Programmieren der Geräte funktioniert. Somit ist hier noch kein Zeitpunkt geplant.  
  
# Installation
Es gibt 2 Möglichkeiten die kompilierte Anwendung zu installieren:  
 - [Microsoft Store](https://www.microsoft.com/store/productId/9NX69NJ80X6T)  
    Hier werden nur größere Releases veröffentlicht
 - [App Installer File](https://kaenx.mikegerst.de)  
    Hier werden auch kleiner Releases veröffentlicht. Die App sucht automatisch nach neuen Updates und installiert diese.  
    Es ist zwingend erforderlich, das ["Herausgeber Zertifikat"](https://kaenx.mikegerst.de/Updater/Kaenx_0.0.55.0_Test/Kaenx_0.0.55.0_x86_x64.cer) als Administrator zu Installieren. Als Speicherort ist der lokale Computer und "Vertrauenswürdige Stammzertifizierungsstelle" zu wählen.

# Bilder
[![Kaenx Ansicht Topologie](/Images/Topologie.png)](/Images/)
  
# Datenschutz
Die App sammelt anonymisierte Daten zu Abstürzen und ein paar definierter Ereignisse über App-Insights und App-Center (Daten werden bis zu 90 Tage gespeichert).  
Weiter Infos über App-Insight finden sie [hier](https://docs.microsoft.com/de-de/azure/azure-monitor/app/data-retention-privacy).