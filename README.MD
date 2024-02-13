# BulkImportSQL Command Line Utility

This Markdown documentation explains how to use the `BulkImportSQL` Command Line Utility. It is a handy utility designed for .NET applications, which lets you import JSON content into SQL databases.

This library allows handling command-line arguments with ease and supports both required and optional arguments.

The main features of this library are:
* Easy command line parsing functionality
* Options to define both required and optional command-line arguments
* Support for identifying the presence or absence of a specific argument
* Easy error handling mechanism

## Table of Contents

1. [Required Arguments](#required-arguments)
2. [Optional Arguments](#optional-arguments)
3. [Command Line Example](#command-line-example)
4. [Handling Errors](#handling-errors)
5. [Additional Considerations](#additional-considerations)

## Required Arguments

Each required argument should be passed on the command line to run the tool.
Following are the required arguments:

| Short | Long | Description |
| :---- | :--- | :---------- |
| -i | --input | The input of the file to be imported |
| -s | --server | The server to connect to |
| -d | --database | The database to connect to |
| -t | --table | The table to import to |
| -u | --username | The username to connect with |
| -p | --password | The password to connect with |

## Optional Arguments
In addition to the required arguments, the tool provides some optional ones. Here are those:

| Short | Long | Description |
| :---- | :--- | :---------- |
| -c | --columns | The columns to import, if not specified, all columns will be imported. The columns should be separated by a comma. Make sure to url encode this argument. Ex: column1,column2,column3%20with%20spaces |
| -e | --element | If the input json has a sub element, specify the element to import. |
| -b | --batch | The batch size to import. Default is 1000 |
| -ps | --processes | The number of processes to use. Default is the available processors count |
| -j | --json | To output the results in json format, specify this flag and a file name. Ex: -j results.json |
| -sm | --silent | This mode will not print any output to the console, perfect for headless operations |
| -cp | --port | The port to connect to the server. Default is 3306 |
| -tt | --test | This mode will not insert any data into the database, perfect for testing the connection and parsing the input file |
| -n | --empty | This will empty the table before inserting data |

## Command Line Example
Here is an example of a command line syntax using these options:

```bash
BulkImportSQL -i input.json -s myserver -d mydatabase -t mytable -u myuser -p mypassword -c column1,column2 -e myelement -b 2000 -ps 4 -j output.js -sm -cp 3306 -tt
```

## Handling Errors
In case of invalid command line parameters, the application stops execution and displays an error message. This includes scenarios like the absence of required parameters or invalid data types for specific parameters.

## Additional Considerations
Please note that this tool will perform SQL operations and it's recommended to be careful when specifying the arguments to avoid any unwanted data loss or manipulation.

Enjoy your experience with the BulkImportSQL Command Line Utility!

