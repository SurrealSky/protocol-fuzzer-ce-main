import xml.sax.handler

from waflib.Configure import conf

class PackageHandler(xml.sax.handler.ContentHandler):
	def __init__(self, bld, excl, mapping):
		self.bld = bld
		self.excl = excl
		self.mapping = mapping

	def startElement(self, name, attrs):
		if name != 'package':
			return

		bld = self.bld
		name = str(attrs['id'])
		version = str(attrs['version'])
		target = str(attrs['targetFramework'])

		expected_dir = '%s.%s' % (name, version)
		path = bld.path.find_dir([expected_dir])

		if not path:
			raise Exception("Unable to find package directory '%s'; verify package 'id' and 'version' attributes in 'packages.config'." % expected_dir)

		basename = bld.env.BASENAME
		if self.mapping:
			opts = self.mapping.get(name, basename)
			if isinstance(opts, str):
				opts = [ opts ]
			if basename not in opts:
				return

		pat = 'lib/*.dll lib/net/*.dll lib/%s/*.dll' % target

		excl = self.excl and self.excl.get(name, None) or None
		if excl:
			nodes = path.ant_glob(pat, excl=excl, ignorecase=True)
		else:
			nodes = path.ant_glob(pat, ignorecase=True)

		for n in nodes:
			bld.read_csshlib(n.name, paths = [ n.parent ])

		content = path.find_dir('content')
		if content:
			bld(name=name, path=content, content=content.ant_glob('**/*'))

@conf
def read_nuget(self, config, excl=None, mapping=None):
	"""
	Parse nuget packages.config and run read_csslib on each line
	"""

	if not self.env.MCS:
		return

	src = self.path.find_resource(config)
	if src:
		handler = PackageHandler(self, excl, mapping)
		parser = xml.sax.make_parser()
		parser.setContentHandler(handler)
		parser.parse(src.abspath())
