#import <Cocoa/Cocoa.h>
#include <string.h>
// Called on Unity's main thread. Selection stays local until the explicit upload action.
const char* OdysseyChoosePortrait(void) {
 @autoreleasepool {
  NSOpenPanel *panel=[NSOpenPanel openPanel];
  panel.title=@"Choose your Albion Odyssey portrait";
  panel.allowedFileTypes=@[@"png",@"jpg",@"jpeg"];
  panel.allowsMultipleSelection=NO;panel.canChooseDirectories=NO;
  if([panel runModal]!=NSModalResponseOK)return NULL;
  return strdup(panel.URL.path.UTF8String);
 }
}
void OdysseyFreePortraitPath(const char* path){free((void*)path);}
