# Create your own kafka cluster securised with your own CA

This repository explains how create and configure a kafka cluster with your own certificate authority.
In my case, I have only one broker and I configure it with SASL PLAIN authentification.

Please find samples broker configuration and certificates in samples folder. 

Follow these steps to create a kafka cluster with SSL and a consumer/producer SSL configured.

## Securise Cluster

### Create Root CA

1. Generate a private key named root.key

``` bash
openssl genrsa -out root.key
Generating RSA private key, 2048 bit long modulus
...............................................................+++
...................+++
e is 65537 (0x10001)
```

2. Generating a self-signed root CA named root.crt

```bash
openssl req -new -x509 -key root.key -out root.crt
You are about to be asked to enter information that will be incorporated
into your certificate request.
What you are about to enter is what is called a Distinguished Name or a DN.
There are quite a few fields but you can leave some blank
For some fields there will be a default value,
If you enter '.', the field will be left blank.
-----
Country Name (2 letter code) [XX]:FR
State or Province Name (full name) []:France
Locality Name (eg, city) [Default City]:Clermont-Ferrand
Organization Name (eg, company) [Default Company Ltd]:My Compagny
Organizational Unit Name (eg, section) []:
Common Name (eg, your name or your server's hostname) []:*mycompagny.com
```

### Create the Truststore and Keystore

1. Create a truststore file for all of the Kafka brokers. In this example, this truststore only needs to contain the root CA created earlier, as it is used to sign all of the certificates in this example. If you are not using a single CA to sign all of the certificates, add all of the CAs you used to sign the other certificates.

All of the brokers can use the same truststore file.

``` bash
keytool -keystore kafka.truststore.jks -alias CARoot -import  -file root.crt
Enter keystore password: 123456789
Re-enter new password: 123456789
Owner: CN=*mycompagny.com, O=My Compagny, L=Clermont-Ferrand, ST=France, C=FR
Issuer: CN=*mycompagny.com, O=My Compagny, L=Clermont-Ferrand, ST=France, C=FR
Serial number: eb3e51d8bbf47863
Valid from: Wed May 27 05:19:54 EDT 2020 until: Fri Jun 26 05:19:54 EDT 2020
Certificate fingerprints:
         MD5:  29:BB:2F:5B:88:83:07:67:F5:25:F6:8C:6A:64:9F:BE
         SHA1: E4:F7:5B:43:27:48:AB:4C:FA:C3:11:80:EC:96:15:4D:CF:94:F8:50
         SHA256: 8A:A2:DC:3C:D9:2A:67:7C:9A:A2:26:3B:72:82:7F:8A:E1:C2:7C:88:72:CB:2F:3B:2E:37:AA:45:8D:72:FA:6C
Signature algorithm name: SHA256withRSA
Subject Public Key Algorithm: 2048-bit RSA key
Version: 3

Extensions:

#1: ObjectId: 2.5.29.35 Criticality=false
AuthorityKeyIdentifier [
KeyIdentifier [
0000: CA 5E 1B 27 D3 03 C8 81   12 09 45 67 05 4F 47 1C  .^.'......Eg.OG.
0010: CE D9 35 B3                                        ..5.
]
]

#2: ObjectId: 2.5.29.19 Criticality=false
BasicConstraints:[
  CA:true
  PathLen:2147483647
]

#3: ObjectId: 2.5.29.14 Criticality=false
SubjectKeyIdentifier [
KeyIdentifier [
0000: CA 5E 1B 27 D3 03 C8 81   12 09 45 67 05 4F 47 1C  .^.'......Eg.OG.
0010: CE D9 35 B3                                        ..5.
]
]

Trust this certificate? [no]:  yes
Certificate was added to keystore
```

2. Create a keystore file for the Kafka broker named kafka01. Each broker gets its own unique keystore. The keytool command in the following example adds a Subject Alternative Name (SAN) to act as a fall back when performing SSL authentication. Use the fully-qualified domain name (FQDN) of your Kafka broker as the value for this option and your response to the "What is your first and last name?" prompt. In this example, the FQDN of the Kafka broker is kafka01.mycompany.com. The alias for the keytool is set to localhost, so local connections on the broker can authenticate using SSL.

``` bash
keytool -keystore kafka01.keystore.jks -alias localhost -validity 365 -genkey -keyalg RSA -ext SAN=DNS:kafka01.mycompany.com
Enter keystore password: 123456789
Re-enter new password: 123456789
What is your first and last name?
  [Unknown]:  kafka01.mycompagny.com
What is the name of your organizational unit?
  [Unknown]:
What is the name of your organization?
  [Unknown]:  MyCompagny
What is the name of your City or Locality?
  [Unknown]:  Clermont-Ferrand
What is the name of your State or Province?
  [Unknown]:  France
What is the two-letter country code for this unit?
  [Unknown]:  FR
Is CN=kafka01.mycompagny.com, OU=Unknown, O=MyCompagny, L=Clermont-Ferrand, ST=France, C=FR correct?
  [no]:  yes

Enter key password for <localhost>
        (RETURN if same as keystore password):

Warning:
The JKS keystore uses a proprietary format. It is recommended to migrate to PKCS12 which is an industry standard format using "keytool -importkeystore -srckeystore kafka01.keystore.jks -destkeystore kafka01.keystore.jks -deststoretype pkcs12".
```

3. Export the Kafka broker's certificate so it can be signed by the root CA.

``` bash
keytool -keystore kafka01.keystore.jks -alias localhost -certreq -file kafka01.unsigned.crt
Enter keystore password: 123456789

Warning:
The JKS keystore uses a proprietary format. It is recommended to migrate to PKCS12 which is an industry standard format using "keytool -importkeystore -srckeystore kafka01.keystore.jks -destkeystore kafka01.keystore.jks -deststoretype pkcs12".
```

4. Sign the Kafka broker's certificate using the root CA.

``` bash
openssl x509 -req -CA root.crt -CAkey root.key -in kafka01.unsigned.crt -out kafka01.signed.crt -days 365 -CAcreateserial
Signature ok
subject=/C=FR/ST=France/L=Clermont-Ferrand/O=MyCompagny/OU=Unknown/CN=kafka01.mycompagny.com
Getting CA Private Key
```

5. Import the root CA into the broker's keystore.

``` bash
keytool -keystore kafka01.keystore.jks -alias CARoot -import -file root.crt
Enter keystore password: 123456789
Owner: CN=*mycompagny.com, O=My Compagny, L=Clermont-Ferrand, ST=France, C=FR
Issuer: CN=*mycompagny.com, O=My Compagny, L=Clermont-Ferrand, ST=France, C=FR
Serial number: eb3e51d8bbf47863
Valid from: Wed May 27 05:19:54 EDT 2020 until: Fri Jun 26 05:19:54 EDT 2020
Certificate fingerprints:
         MD5:  29:BB:2F:5B:88:83:07:67:F5:25:F6:8C:6A:64:9F:BE
         SHA1: E4:F7:5B:43:27:48:AB:4C:FA:C3:11:80:EC:96:15:4D:CF:94:F8:50
         SHA256: 8A:A2:DC:3C:D9:2A:67:7C:9A:A2:26:3B:72:82:7F:8A:E1:C2:7C:88:72:CB:2F:3B:2E:37:AA:45:8D:72:FA:6C
Signature algorithm name: SHA256withRSA
Subject Public Key Algorithm: 2048-bit RSA key
Version: 3

Extensions:

#1: ObjectId: 2.5.29.35 Criticality=false
AuthorityKeyIdentifier [
KeyIdentifier [
0000: CA 5E 1B 27 D3 03 C8 81   12 09 45 67 05 4F 47 1C  .^.'......Eg.OG.
0010: CE D9 35 B3                                        ..5.
]
]

#2: ObjectId: 2.5.29.19 Criticality=false
BasicConstraints:[
  CA:true
  PathLen:2147483647
]

#3: ObjectId: 2.5.29.14 Criticality=false
SubjectKeyIdentifier [
KeyIdentifier [
0000: CA 5E 1B 27 D3 03 C8 81   12 09 45 67 05 4F 47 1C  .^.'......Eg.OG.
0010: CE D9 35 B3                                        ..5.
]
]

Trust this certificate? [no]:  yes
Certificate was added to keystore

Warning:
The JKS keystore uses a proprietary format. It is recommended to migrate to PKCS12 which is an industry standard format using "keytool -importkeystore -srckeystore kafka01.keystore.jks -destkeystore kafka01.keystore.jks -deststoretype pkcs12".
```

6. Import the signed Kafka broker certificate into the keystore.

``` bash
keytool -keystore kafka01.keystore.jks -alias localhost -import -file kafka01.signed.crt
Enter keystore password:
Certificate reply was installed in keystore

Warning:
The JKS keystore uses a proprietary format. It is recommended to migrate to PKCS12 which is an industry standard format using "keytool -importkeystore -srckeystore kafka01.keystore.jks -destkeystore kafka01.keystore.jks -deststoretype pkcs12".```
```

7. Copy truststore and store in each kafka broker configuration folder (/etc/kafka by default)

``` bash
cp kafka.truststore.jks /etc/kafka/
cp kafka01.keystore.jks /etc/kafka/
```

### Configure Kafka

1. Edit the Kafka Configuration to Use TLS/SSL Encryption

Please edit server.properties present in kafka configuration folder (/etc/kafka by default)

|Setting|Description|
|---|---|
|listeners|The host names and ports on which the Kafka broker listens.|
|ssl.keystore.location|The absolute path to the keystore file.|
|ssl.keystore.password|The password for the keystore file.|
|ssl.key.password|The password of the private key in the key store file.|
|ssl.truststore.location|The location of the truststore file.|
|ssl.truststore.password|The password to access the truststore.|
|ssl.enabled.protocols|The TLS/SSL protocols that Kafka allows clients to use.|
|ssl.client.auth|Whether SSL authentication is required or optional. The most secure setting for this setting is required to verify the client's identity.|
|ssl.endpoint.identification.algorithm|The endpoint identification algorithm to validate server hostname using server certificate.(Default : https, set empty string if you don't want verification)|
|ssl.keystore.type|The file format of the key store file.|
|ssl.truststore.type|The file format of the trust store file.|

You can found more informations [here](https://kafka.apache.org/documentation/#configuration)

My configuration :

```
advertised.listeners=SASL_SSL://192.168.56.1:9093
listeners=SASL_SSL://10.0.2.15:9093
ssl.truststore.location=/etc/kafka/kafka.truststore.jks
ssl.truststore.password=123456789
ssl.keystore.location=/etc/kafka/kafka01.keystore.jks
ssl.keystore.password=123456789
ssl.key.password=123456789
ssl.enabled.protocols=TLSv1.2,TLSv1.1,TLSv1
ssl.client.auth=required
ssl.endpoint.identification.algorithm=
ssl.keystore.type=JKS
ssl.truststore.type=JKS
```

2. Edit the Kafka Configuration to use SASL PLAIN Authentification

Please edit server.properties present in kafka configuration folder (/etc/kafka by default)

|Setting|Description|
|---|---|
|security.inter.broker.protocol|Security protocol used to communicate between brokers. Valid values are: PLAINTEXT, SSL, SASL_PLAINTEXT, SASL_SSL. It is an error to set this and inter.broker.listener.name properties at the same time.|
|sasl.mechanism.inter.broker.protocol|SASL mechanism used for inter-broker communication. Default is GSSAPI.|
|sasl.enabled.mechanisms|The list of SASL mechanisms enabled in the Kafka server. The list may contain any mechanism for which a security provider is available. Only GSSAPI is enabled by default.|

You can found more informations about SASL Authentificaton [here](https://docs.confluent.io/current/kafka/authentication_sasl/index.html)

My configuration :

```
security.inter.broker.protocol=SASL_SSL
sasl.mechanism.inter.broker.protocol=PLAIN
sasl.enabled.mechanisms=PLAIN
```

You can found configuration files in samples\broker-configuration.

## Test with producer/consumer securised

### Create Consumer certificate

### Create Producer certificate