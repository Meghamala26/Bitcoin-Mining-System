# Bitcoin-Mining-System

The Bitcoin Mining system has 2 main components- Server and Client to mine for bitcoins. We have used the Actor Model in F# to implement a distributed system to look for the coins. The Server can independently find coins that conform to the leading number of zeroes shared by the user as input. For this, it makes the use of 3 types of actors - the Master which supervises over the entire mining process, the CoinBucket which prints the bitcoins found and the Worker which finds the valid coins that matches with the leading number of zeroes. The workers are created in a Round Robin Pool fashion based on the maximum number of processors available to the system.  The Worker creates random strings concatenated with the Gator id “m.gupta1”, hashes it using the SHA256 algorithm and checks for the required number of leading zeros in it. Once found, it sends the coin to the CoinBucket for printing.  

The Server by default mines for coins itself. The Client can only request for work from the Server. It cannot mine for coins by itself. On receiving the request from Client’s Master, the Server’s master sends the gator id + noOfZeros as input to the Client’s master. The client on finding any bitcoins sends it back to the Server to be printed. The work is distributed among all the workers employed for mining and the distribution is capped by the maximum number of processors available to the system.

Parallelism is achieved as each worker creates a random string for itself to mine, thus not being limited to any given work unit. The probability of more than one worker generating the same random string being very low, every worker works independently on different random strings, just distributing the mining process among all the workers.
