/**
 * Deep console log helper for Cloudflare Workers
 * Ensures all object properties are fully logged in JSON format
 */
interface DeepConsoleLogOptions {
	label?: string;
	maxDepth?: number;
	currentDepth?: number;
}

function deepConsoleLog(
	obj: unknown,
	label: string = 'Object',
	maxDepth: number = 10,
	currentDepth: number = 0
): void {
	const result = serializeToJson(obj, maxDepth, currentDepth);
	console.log(JSON.stringify({ [label]: result }, null, 2));
}

/**
 * Recursively serialize an object to JSON-safe format
 */
function serializeToJson(obj: any, maxDepth: number = 10, currentDepth: number = 0, seen: WeakSet<any> = new WeakSet()): any {
  // Prevent infinite recursion
  if (currentDepth >= maxDepth) {
    return "[Max depth reached]";
  }

  // Handle null/undefined
  if (obj === null || obj === undefined) {
    return obj;
  }

  // Handle primitives
  if (typeof obj !== 'object' && typeof obj !== 'function') {
    return obj;
  }

  // Handle circular references
  if (typeof obj === 'object' && obj !== null) {
    if (seen.has(obj)) {
      return "[Circular Reference]";
    }
    seen.add(obj);
  }

  // Handle functions
  if (typeof obj === 'function') {
    return {
      __type: "Function",
      name: obj.name || "anonymous",
      length: obj.length,
      toString: obj.toString()
    };
  }

  // Handle arrays
  if (Array.isArray(obj)) {
    return {
      __type: "Array",
      length: obj.length,
      items: obj.map((item, index) => ({
        index,
        value: serializeToJson(item, maxDepth, currentDepth + 1, seen)
      }))
    };
  }

  // Handle special object types
  if (obj instanceof Date) {
    return {
      __type: "Date",
      value: obj.toISOString(),
      timestamp: obj.getTime()
    };
  }

  if (obj instanceof RegExp) {
    return {
      __type: "RegExp",
      pattern: obj.source,
      flags: obj.flags,
      toString: obj.toString()
    };
  }

  if (obj instanceof Error) {
    return {
      __type: "Error",
      name: obj.name,
      message: obj.message,
      stack: obj.stack
    };
  }

  // Handle objects
  const result: {
    __type: string;
    properties: Record<string, any>;
    [key: string]: any; // Allow additional properties like __prototype
  } = {
    __type: obj.constructor?.name || "Object",
    properties: {}
  };

  try {
    // Get all property names (including non-enumerable)
    const allKeys = [
      ...Object.getOwnPropertyNames(obj),
      ...Object.getOwnPropertySymbols(obj)
    ];

    // Remove duplicates
    const uniqueKeys = [...new Set(allKeys)];

    uniqueKeys.forEach(key => {
      let keyStr;
      try {
        keyStr = typeof key === 'symbol' ? key.toString() : key;
        const descriptor = Object.getOwnPropertyDescriptor(obj, key);

        if (descriptor) {
          if (descriptor.get || descriptor.set) {
            result.properties[keyStr] = {
              __type: "Property",
              hasGetter: !!descriptor.get,
              hasSetter: !!descriptor.set,
              enumerable: descriptor.enumerable,
              configurable: descriptor.configurable
            };
          } else if (descriptor.value !== undefined) {
            result.properties[keyStr] = {
              value: serializeToJson(descriptor.value, maxDepth, currentDepth + 1, seen),
              enumerable: descriptor.enumerable,
              writable: descriptor.writable,
              configurable: descriptor.configurable
            };
          } else {
            result.properties[keyStr] = {
              __type: "Property",
              descriptor: "No value",
              enumerable: descriptor.enumerable,
              configurable: descriptor.configurable
            };
          }
        } else {
          result.properties[keyStr] = "[No descriptor found]";
        }
      } catch (error) {
        const errorMsg = (error && typeof error === "object" && "message" in error) ? (error as any).message : String(error);
        result.properties[keyStr ?? String(key)] = `[Error accessing property: ${errorMsg}]`;
      }
    });

    // Add prototype information
    const proto = Object.getPrototypeOf(obj);
    if (proto && proto !== Object.prototype && proto !== Array.prototype) {
      result.__prototype = serializeToJson(proto, Math.min(maxDepth, 2), currentDepth + 1, seen);
    }

  } catch (error) {
    result.__error = `Serialization error: ${error instanceof Error ? error.message : String(error)}`;
  }

  return result;
}

/**
 * Enhanced console.log that works well in Cloudflare Workers
 * Automatically formats everything as JSON
 */
interface CfLogArgument {
	[key: string]: any;
}

interface CfLogOutput {
	message: string;
	timestamp: string;
	arguments: CfLogArgument[];
}

function cfLog(...args: unknown[]): void {
	if (!args || args.length === 0) {
		console.log("No arguments provided to cfLog");
		return;
	}
	const jsonOutput: CfLogOutput = {
		message: (args[0] as string) || "Log message",
		timestamp: new Date().toISOString(),
		arguments: args.map((arg, index) => ({
			[`arg${index}`]: serializeToJson(arg, 10, 0)
		}))
	};

	console.log(JSON.stringify(jsonOutput, null, 2));
}

/**
 * Log object with guaranteed JSON output
 * Handles circular references and all special values
 */
interface SafeStringifyLogOutput {
	timestamp: string;
	label: string;
	data: unknown;
}

function safeStringifyLog(obj: unknown, label: string = 'Object'): void {
	const jsonOutput: SafeStringifyLogOutput = {
		timestamp: new Date().toISOString(),
		label: label,
		data: serializeToJson(obj, 10, 0)
	};

	console.log(JSON.stringify(jsonOutput, null, 2));
}

// Export for use in Cloudflare Workers
export { deepConsoleLog, cfLog, safeStringifyLog };
