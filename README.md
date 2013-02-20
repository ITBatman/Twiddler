Twiddler 
======================

Developed by Richard Reukema - using various bits from the internet, which I have noted in my code
Named by Devin Rader - in his review of the code base.

The project has come to me very, very slowly, so although I'm not proud of some of the code (validations, message boxes, etc.), or the UI (it works for me :), I am proud of what it does.

Using the Azure Service Bus, this Fiddler extension will easily allow the routing of a Twilio Web Hook (or any web hook, but I built this for me Twilio development projects), to a developers machine.  As Twilio web hook must be to a public endpoint, the startup or playing of a Twilio application is difficult, as most developers don't have a web server in the public namespace.

I've created a couple of blog posts to describe the development effort, as well as the configuration. 
See:

http://itbatman.wordpress.com/2013/02/03/twiddler_intro/

followed by
http://itbatman.wordpress.com/2013/02/05/twiddler_playing_catchup/

followed by the lengthy and detailed post to configure the extension
http://itbatman.wordpress.com/2013/02/05/twiddler_configuration/

