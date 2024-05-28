extern UIViewController *UnityGetGLViewController();

NSString *ToNSString(char* string) {
    return [NSString stringWithUTF8String:string];
}

typedef void (*AlertButtonCallback)(const char * str);

void AlertMessage (bool isSheet, char* title, char* message, char* buttonsText[], int buttonsStyle[], int buttonsLength, AlertButtonCallback buttonCallback) 
{
    UIAlertControllerStyle style = isSheet ? UIAlertControllerStyleActionSheet : UIAlertControllerStyleAlert;
    UIAlertController *alert = [UIAlertController alertControllerWithTitle:ToNSString(title) message:ToNSString(message) preferredStyle:style];

    for (int i = 0; i < buttonsLength; i++) 
    {
        NSString *buttonText = ToNSString(buttonsText[i]);
        int index = i;
        UIAlertAction * button = [UIAlertAction actionWithTitle:buttonText style:(UIAlertActionStyle)buttonsStyle[i] handler:^(UIAlertAction * action) 
        {
            buttonCallback((char*)[buttonText UTF8String]);
        }];
        [alert addAction:button];
    }
    
    dispatch_async(dispatch_get_main_queue(), ^{
        [UnityGetGLViewController() presentViewController:alert animated:YES completion:nil];
    });
}

void ToastMessage (char* message, BOOL isLongDuration) 
{
    float duration = isLongDuration ? 3.5 : 2;
    UIAlertController *alert = [UIAlertController alertControllerWithTitle:nil message:ToNSString(message) preferredStyle:UIAlertControllerStyleAlert];
    
    [UnityGetGLViewController() presentViewController:alert animated:YES completion:nil];
    
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, duration * NSEC_PER_SEC), dispatch_get_main_queue(), ^{
        [alert dismissViewControllerAnimated:YES completion:nil];
    });
}
