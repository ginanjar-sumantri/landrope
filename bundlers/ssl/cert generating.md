# Generate self-signed certificate with ECDSA using two common curves
openssl req -x509 -nodes -days 3650 -newkey ec:<(openssl ecparam -name prime256v1) -keyout ecdsakey.pem -out ecdsacert.pem
openssl req -x509 -nodes -days 3650 -newkey ec:<(openssl ecparam -name secp384r1) -keyout ecdsakey.pem -out ecdsacert.pem

# print private and public key + curve name
openssl ec -in ecdsakey.pem -text -noout

# print certificate
openssl x509 -in ecdsacert.pem -text -noout

# generate container
openssl pkcs12 -export -inkey ecdsakey.pem -in ecdsacert.pem -out ecdsacred.p12
