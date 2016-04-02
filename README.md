
![Image of Stahp Dog](https://raw.githubusercontent.com/TechnikEmpire/StahpIt-WPF/master/Stahp%20It/Resources/Media/StahpIt.fw.png)  


Stahp It is a transparent HTTP/S Content Filter that uses Adblock Plus filters and CSS selectors, for Windows Vista and later. Using Stahp It, it's possible to filter HTTP requests made by any process on your device. 

Stahp It also detectes HTML payloads in HTTP responses, parses them, and removes elements by using user provided CSS selectors before returning the content. So it's possible to not just filter requests, but modify the content returned by those requests.

Put simply, Stahp It gives users complete control over all HTTP traffic. By its nature, every single browser that exists today is supported, and any browser in the future would automatically be supported. This is because Stahp It resides at the packet level, sitting atop the hardware, gaining automatic precedence.

Since any process of any kind that does HTTP/S communication is supported, so you can filter web content that otherwise escapes other blockers (looking at you Windows 8/8.1/10 and the ads you ~~inject~~ used to inject into my shell).  

##Privacy  
Stahp It does not communicate with any external machine, except in its function as a transparent proxy. Personal settings are not stored anywhere but on your device, same goes for statistics and any other application related data. No third party devices are used for any sort of processing.

##Legalities  
Stahp It includes binary releases of OpenSSL. Below is a quote taken from the [downloads page](https://www.openssl.org/source/) for OpenSSL:

> Please remember that export/import and/or use of strong cryptography software, providing cryptography hooks, or even just > communicating technical details about cryptography software is illegal in some parts of the world. So when you import this > package to your country, re-distribute it from there or even just email technical suggestions or even source patches to the > authors or other people you are strongly advised to pay close attention to any laws or regulations which apply to you. The > authors of openssl are not liable for any violations you make here. So be careful, it is your responsibility. 

I've quoted this warning because, seeing how Stahp It includes OpenSSL, this applies to your download/use of Stahp It.  

Given that Stahp It can and will intercept and process HTTPS (secured, encrypted) connections by design, it is recommended that you only install Stahp It on your own personal device(s). Also, being that this software is "exported" from Canada, I'll also state that Stahp It is not "security software" in any sense, especially as defined by EXCOL laws and regulations. Download/usage of Stahp It is permitted only in strict adherance with the terms of the [License](https://raw.githubusercontent.com/TechnikEmpire/StahpIt-WPF/master/LICENSE), which is the GPLv3 or any later version with a special exception for OpenSSL. 

Finally, I am not a lawyer, I am not your lawyer. None of this is legal advice. This is a cautionary commentary that you should comply with all International, National and local laws, as well as the License when using this software.
