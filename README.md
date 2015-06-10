Raven.Plugins
=============

This repo demonstrate how to implement a custom bundle for [RavenDB] (https://github.com/ravendb/ravendb) in order to achieve collection level expiration.  
Collection level expiration is different than the built-in expiration bundle by letting the user to define expiration  
per collection without marking each document with expiry timestamp.  

For the demonstration I've inherit the "AbstractBackgroundTask" class that implement the "IStartupTask" interface.  
Another thing you can find here is how to log your traces/exceptions using RavenDB logger.  

Read more:
* http://ravendb.net/docs/2.0/server/extending/plugins
