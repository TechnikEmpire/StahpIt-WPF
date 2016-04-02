
![Image of Stahp Dog](https://raw.githubusercontent.com/TechnikEmpire/StahpIt-WPF/master/Stahp%20It/Resources/Media/StahpIt.fw.png)  


Stahp It is a transparent HTTP/S Content Filter that uses Adblock Plus filters and CSS selectors, for Windows Vista and later. Using Stahp It, it's possible to filter HTTP requests made by any process on your device. 

Stahp It also detectes HTML payloads in HTTP responses, parses them, and removes elements by using user provided CSS selectors before returning the content. So it's possible to not just filter requests, but modify the content returned by those requests.

Put simply, Stahp It gives users complete control over all HTTP traffic. By its nature, every single browser that exists today is supported, and any browser in the future would automatically be supported. This is because Stahp It resides at the packet level, sitting atop the hardware, gaining automatic precedence.

Since any process of any kind that does HTTP/S communication is supported, so you can filter web content that otherwise escapes other blockers (looking at you Windows 8/8.1/10 and the ads you ~~inject~~ used to inject into my shell).  

##How It Works  
Stahp It, via the [HttpFilteringEngine](https://github.com/TechnikEmpire/HttpFilteringEngine) library, uses [WinDivert](https://reqrypt.org/windivert.html) to intercept outbound HTTP/S packets and redirect them back inward to the internal proxy. This only happens when the user explicitly enables this functionality and it is controlled on a per-process basis.

Once the traffic is sent back inbound to the proxy, the proxy "pretends" to be the server that the application was attempting to connect to. At this stage, if the connection is a HTTPS connection (secured), the proxy authenticates itself using a one time CA certificate and associated private key.  

This decrypts the HTTPS connection locally, but only as far as Stahp It is concerned. Since we love encryption are not evil, the connection to the external machine (the real server) is in fact secured, and if the process of establishing a secure connection encounters any issues (such as a bad or invalid certificate), the connection is immediately terminated, handing the issue down to the user. 

If this process succeeds, the proxy simply hands data back and forth between the real server and the local process (your browser), only scanning the headers of requests and responses and filtering based on this data. In the event that a HTML response is detected, this content is parsed and all CSS selectors loaded are used to remove content from that payload, before being sent off to the local process (your browser).  

The CA certificate and private key are destroyed and recreated every time the program runs and exits, and the keys are generated using named curves.

##I don't like words like "decrypt."  
Have you ever visited a HTTPS website and saw intelligible content in your browser? That's because your browser decrypted it.

##How Can I Trust You?
Surely, you shouldn't. I'm a random person in the world. All of Stahp It can be built from source and every bit of source code is 100% available for review. This project is just the GUI, the core functionality exists in [GQ](https://github.com/TechnikEmpire/GQ) and [HttpFilteringEngine](https://github.com/TechnikEmpire/HttpFilteringEngine). 

##Privacy  
Stahp It does not communicate with any external machine, except in its function as a transparent proxy. Personal settings are not stored anywhere but on your device, same goes for statistics and any other application related data. No third party devices are used for any sort of processing.

The only "exception" to this is that Stahp It will, on startup, check directly with this repository for updates via [WinSparkle](https://winsparkle.org/). In this case, WinSparkle downloads information about the latest Release via an [appcast](https://github.com/TechnikEmpire/StahpIt-WPF), which is stored in this repository. WinSparkle will then compare this to the installed version and ask the user if they want to upgrade or not. 

##Legalities  
Stahp It includes binary releases of OpenSSL. Below is a quote taken from the [downloads page](https://www.openssl.org/source/) for OpenSSL:

> Please remember that export/import and/or use of strong cryptography software, providing cryptography hooks, or even just communicating technical details about cryptography software is illegal in some parts of the world. So when you import this package to your country, re-distribute it from there or even just email technical suggestions or even source patches to the authors or other people you are strongly advised to pay close attention to any laws or regulations which apply to you. The authors of openssl are not liable for any violations you make here. So be careful, it is your responsibility. 

I've quoted this warning because, seeing how Stahp It includes OpenSSL, this applies to your download/use of Stahp It.  

Given that Stahp It can and will intercept and process HTTPS (secured, encrypted) connections by design, it is recommended that you only install Stahp It on your own personal device(s). Also, being that this software is "exported" from Canada, I'll also state that Stahp It is not "security software" in any sense, especially as defined by EXCOL laws and regulations. Download/usage of Stahp It is permitted only in strict adherance with the terms of the [License](https://raw.githubusercontent.com/TechnikEmpire/StahpIt-WPF/master/LICENSE), which is the GPLv3 or any later version with a special exception for OpenSSL. 

Finally, I am not a lawyer, I am not your lawyer. None of this is legal advice. This is a cautionary commentary that you should comply with all International, National and local laws, as well as the License when using this software. I, the Author am not responsible for any violations you commit, wilful or otherwise.
