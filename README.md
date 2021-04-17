# BashAPI
_by Viiinzzz_

The idea is simple : leverage .Net Core Web API to run a system command (via bash) such as a file converter.

So POST a file via http with appropriate arguments and get back the processed file.

Of course it's a POC, no special security has been implemented to protect the system.

A use case would be to add a Web API to an existing docker image that doesn't have one yet.
