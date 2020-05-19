# DAG Network Layer

## Bootstrap & discovery Nodes
* GetNode message - request Nodes from random Bootstrap Node - no more
than 2500 Bootstrap Node updates Requested Node last seen time
* Bootstrap return nodes last seen within 3 hours
* If nodes count less than 1000 ask Nodes form another random but not
same Bootstrap Node
* Every 20 min Node send Ping message to random Bootstrap Node and
return Pong message
* When Bootstrap Node receive PING request it's update Requested Node
last seen time

## Event Messages
* When some of Events fired (Current Node create own Event or received
form other Nodes) Current Node take random 50 Nodes from 3 hours last
seen Nodes. If nodes count less than 25, take summary with previously
taken nodes - random Nodes from 24 hours last seen Nodes.
* Send message for selected Nodes and store it to Storage
* If received message (Event) already exist in the Storage - ignore
that message
