# README

This repo is my playground for Azure Durable Functions.

Notes here are to help me remember what the heck I was trying to do if I ever come back to it :-)

## simple

This was an exercise in recreating a simple Durable Functions call to activity functions from scratch

## mock-storage-batch(-2)

These two projects are an exploration of a scenario involving batches of files being uploaded to Blob Storage.

The base scenario used as a starting point is:

* Each batch comes from a single customer and contains a known set of files
* The files that make up a batch vary by customer
* Processing should be initiated once all the files in a batch are present
* Batches should only be processed once

For simplicity, the function to retrieve the list of files required in a batch for a customer is just a hard-coded implementation but could easily be replaced with a call to a data store.

In both cases, HTTP triggers have been used for dev purposes as a simulation for blob triggers to enable easier exploration of event orders. Also, the blob access has been substituted with simple local file access as another simplification for rapid exploration.

mock-storage-batch caters for not receiving an event for every file - it just requires that there is at least one event triggered after the last file is written. I.e. it would handle a case where notifications are batched.

mock-storage-batch-2 requires a trigger for each file but is more efficient in terms of the number of External Events that it raises (which reduces the number of invocations of the orchestrator function instance).

In both projects there is a scripts folder with powershell scripts to exercise different scenarios.
