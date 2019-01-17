# DIDA-TUPLE

to run smr server and script client:
	run server from server-smr folder project
	on server console insert : port and server name
	run script-client.exe from its folder project
	on client console insert : server port, server name and name of the script file
	(scripts should be inside the folder "scripts" inside script-client project folder)

What our solution does succesfully:
	- both smr and xl server are able to add and read tuples.
	- smr can take tuples
 	- puppetmaster can start servers and clients, can run all its commands except status
 	- client-scripts is fully implemented 
Problems to solve:
	- we cant yet establish communication between servers
	- xl server still has some bugs on the take method
	- puppetmaster doenst run scripts
	- in the beggining the client can only communicate with one server (if the asked server 
	  is down the client cant establish connecting with other..)

Concluding:
	We can already run several clients and using smr server we have all basic functionallities 
	working except the communication between servers; using xl servers we have the same problem 
        and can only execute the commands add and read

Nota:
	We had some trouble interpreting the project in terms of the read method response.
