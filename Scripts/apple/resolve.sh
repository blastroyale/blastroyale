#!/bin/bash

# Function to verify the signature
verify_signature() {
    local data="$1"
    local signature="$2"
    local public_key_url="$3"

    # Fetch the certificate
    ssl_certificate=$(curl -s "$public_key_url")

    # Convert the certificate to PEM format
    pem=$(echo "$ssl_certificate" | base64 | awk '{print}' ORS='\n' | sed 's/\(.*\)/-----BEGIN CERTIFICATE-----\n\1\n-----END CERTIFICATE-----/')

    # Extract the public key from the PEM certificate
    pubkey_file=$(mktemp)
    echo "$pem" | openssl x509 -pubkey -noout > "$pubkey_file"

    # Verify the signature
    verify_result=$(echo -n "$data" | openssl dgst -sha256 -verify "$pubkey_file" -signature <(echo -n "$signature" | base64 -d))

    # Clean up
    rm "$pubkey_file"

    # Output the result
    if [[ "$verify_result" == "Verified OK" ]]; then
        echo "Signature is valid."
    else
        echo "Signature is invalid."
    fi
}

set -x

user_id="A:_fa804cf2a813ae52f4f9741bb822ca98"
timestamp="1739890245060"
salt="Rug+0Q=="
bundle_id="com.firstlightgames.blastroyale"
public_key_url="https://static.gc.apple.com/public-key/gc-prod-10.cer"  # Replace with the actual URL
signature="XSz13DhlISq1qPRB2mSVTTRmly2beZ0IwVAu1Pmbj9vnLXDt8MRXA0rFKdzqURWJi56rKSofHbS7s/YnTayin0D7WhQUEzKKdkZ5UD7DujwCU2LfAYhUy7rlumyC4S5C7k2M+82JdO1Nb63I7Smrl1QUtVZGAlahJCcf+D46S4426aZ0rUcnSwL3G86SOqIC7Yhok2huS4qhLfQKLErY4z65UocZAMEUYI4zrb7mOZNZfGq/5TAiuwozvpKLJZJguybfodQ6OoINa6RZ6sHeRprLuFhrdBr//MdrLEYESKzBHhCJ51ePmyZufQZcAktfrDli23ZTHKzReIZd9FxjtGGumvVOU8vYEBTPfWBjHWoIcwtkZ6bcRoiyx2BPMY6TUeD50RNfby9PMf8xNb3nw8uNl0nB3WOJEgN4NOXJlUDtqBKF874T5HOlv2uOxSEJoSCd24FBSEYXG4QELQ/CEvhAP2saUWHPdZN3CM+mJ75xAHR4QoUSBVNGysl5ceq+A5i4Vt3Kj+I+sKpgNfIR2eye+fmksism8P/cti7G/Hg+VA2KfLIrpHhcZslz7FVrl4Qn9/U9bjLqFjkGVe9OqKLTLhMUuZCHeaqxjT0i9KEM+xbNhstJi9dTab3Kx9Up2SUt0cv14m5SK8zBLVaAkPQhdN4eUg14zD1Q+4ZNJYc="



data="$user_id$bundle_id$timestamp$salt;"



# Clean up temporary files
#rm -f ./tmp/certificate.pem ./tmp/signature.bin ./tmp/data.txt
verify_signature "$data" "$signature" "$public_key_url"